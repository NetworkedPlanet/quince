using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Medallion.Shell;
using Microsoft.Extensions.Logging;
using NetworkedPlanet.Quince.Git;
using VDS.RDF;
using VDS.RDF.Configuration;

namespace NetworkedPlanet.Quince.Repository
{
    public class QuinceRepository : IPathResolver
    {
        private readonly string _repositoryPath;
        public IQuinceStore QuinceStore { get; private set; }
        private int _cacheThreshold = DefaultCacheThreshold;
        public  IGraph ConfigGraph { get; }
        public INode RepositoryConfigurationNode { get; private set; }
        private readonly IGitWrapperFactory _gitWrapperFactory;

        public const int DefaultCacheThreshold = 1000;
        private static readonly ILogger Log = QuinceLogging.CreateLogger<QuinceRepository>();

        public QuinceRepository(string repositoryPath, IGraph configurationGraph, IGitWrapperFactory gitWrapperFactory)
        {
            if (!Directory.Exists(repositoryPath))
            {
                Log.LogError("Could not find repository directory at {0}", repositoryPath);
                throw new RepositoryNotFoundException(repositoryPath);
            }
            _repositoryPath = repositoryPath;
            _gitWrapperFactory = gitWrapperFactory;
            ConfigGraph = configurationGraph;
            ReadConfiguration();
        }

        public IQuinceStore GetStore()
        {
            // TODO: Read options from configuration
            return new DynamicFileStore(_repositoryPath, DefaultCacheThreshold);
        }

        private void ReadConfiguration()
        {
            ConfigGraph.NamespaceMap.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            ConfigGraph.NamespaceMap.AddNamespace("q", new Uri("http://networkedplanet.com/ns/quince/"));
            ConfigGraph.NamespaceMap.AddNamespace("sd", new Uri("http://www.w3.org/ns/sparql-service-description#"));

            RepositoryConfigurationNode = GetRepositoryNode(ConfigGraph);
            if (RepositoryConfigurationNode == null)
            {
                Log.LogError("Could not find a repository node in the configuration");
                throw new RepositoryConfigurationException("Could not find a repository node in the configuration.");
            }
            QuinceStore = new DynamicFileStore(_repositoryPath, _cacheThreshold);
        }

        private static INode GetRepositoryNode(IGraph g)
        {
            var rdfType = g.CreateUriNode("rdf:type");
            var quinceRepo = g.CreateUriNode("q:Repository");
            return g.GetTriplesWithPredicateObject(rdfType, quinceRepo).Select(t => t.Subject).FirstOrDefault();
        }

        public string ResolvePath(string path)
        {
            return Path.Combine(_repositoryPath, path);
        }

        public bool ApplyDiff(IDiffResult diffResult)
        {
            // TODO: Make async
            Log.LogTrace("ApplyDiff: {0}", diffResult.ToString());
            var store = GetStore();
            if (!diffResult.Success)
            {
                Log.LogTrace("ApplyDiff: Aborting attempt to apply a failed diff command result");
                return false;
            }
            var updateGraph = new NonIndexedGraph { BaseUri = new Uri("http://example.org/") };
            var parser = new NQuadsParser();
            Log.LogTrace("ApplyDiff: Applying deletes");
            parser.Parse(diffResult.FileDiffs.SelectMany(diff => diff.Deleted),
                triple => store.Retract(triple.Subject, triple.Predicate, triple.Object, triple.GraphUri),
                updateGraph);
            updateGraph.Clear();
            Log.LogTrace("ApplyDiff: Applying inserts");
            parser.Parse(diffResult.FileDiffs.SelectMany(diff => diff.Inserted),
                triple => store.Assert(triple.Subject, triple.Predicate, triple.Object, triple.GraphUri),
                updateGraph);
            Log.LogTrace("ApplyDiff: Flushing changes");
            store.Flush();
            Log.LogTrace("ApplyDiff: Completed");
            return true;
        }

        public async Task<BranchStatus> GetEditBranchStatus(string editBranchName)
        {
            Log.LogTrace("GetEditBranchStatus: {0}", editBranchName);
            var git = _gitWrapperFactory.MakeGitWrapper(_repositoryPath);
            var branchList = await git.ListBranches(editBranchName);
            if (branchList.Length == 0) return null;
            var aheadCount =
                await git.RevListCount("refs/heads/develop..refs/heads/" + editBranchName);
            var behindCount =
                await git.RevListCount("refs/heads/" + editBranchName + "..refs/heads/develop");
            return new BranchStatus(editBranchName, "develop", aheadCount, behindCount);
        }

        public async Task<CreateEditBranchResponse> CreateEditBranch(string editBranchName, string originBranch = "develop")
        {
            Log.LogTrace("CreateEditBranch: {0}, {1}", editBranchName, originBranch);
            var git = _gitWrapperFactory.MakeGitWrapper(_repositoryPath);
            var branches = await git.ListBranches(editBranchName);
            if (branches.Length > 0)
            {
                Log.LogWarning("CreateEditBranch: Conflict with existing branch {0}", editBranchName);
                await git.SetBranch(editBranchName);
                return CreateEditBranchResponse.Conflict;
            }
            var checkout = await git.SetBranch(originBranch);
            if (!checkout.Success)
            {
                LogFailedCommand("CreateEditBranch: Failed to checkout origin branch " + originBranch, checkout);
                return CreateEditBranchResponse.Failed;
            }
            var pull = await git.Pull();
            if (!pull.Success)
            {
                LogFailedCommand("CreateEditBranch: Failed to pull origin branch " + originBranch, pull);
                return CreateEditBranchResponse.Failed;
            }
            checkout = await git.NewBranch(editBranchName, "refs/heads/" + originBranch);
            if (!checkout.Success)
            {
                LogFailedCommand("CreateEditBranch: failed to checkout edit branch " + editBranchName, checkout);
                return CreateEditBranchResponse.Failed;
            }
            return CreateEditBranchResponse.Created;
        }

        public async Task<bool> Commit(string commitMessage, string commitAuthor, string commitBody = null)
        {
            Log.LogTrace("Commit: {0}, {1}", commitMessage, commitAuthor);
            var git = _gitWrapperFactory.MakeGitWrapper(_repositoryPath);
            var addResult = await git.AddAll();
            if (!addResult.Success)
            {
                LogFailedCommand("Commit: failed to stage all changes", addResult);
                return false;
            }
            var commitResult = await git.Commit(commitMessage, author: commitAuthor, body:commitBody);
            if (!commitResult.Success)
            {
                LogFailedCommand("Commit: failed to commit staged changes", commitResult);
            }
            return commitResult.Success;
        }

        public async Task<StartMergeResponse> StartMerge(string editBranchName, string stagingBranchName)
        {
            var git = _gitWrapperFactory.MakeGitWrapper(_repositoryPath);

            // Pull develop branch updates
            var fetchResult = await git.Fetch();
            var developUpdated = fetchResult.UpdatedBranches.Any(b => b.Equals("develop"));
            if (developUpdated)
            {
                // Checkout develop branch
                var checkoutStatus = await git.SetBranch("develop");
                if (!checkoutStatus.Success)
                {
                    LogFailedCommand("Failed to checkout develop branch", checkoutStatus);
                    return new StartMergeResponse("Failed to checkout develop branch.");
                }

                // Reset develop branch to discard any previous attempted merge
                var resetStatus = await git.Reset(GitResetMode.Hard, "origin/develop");
                if (!resetStatus.Success)
                {
                    LogFailedCommand("Failed to reset develop branch", resetStatus);
                    return new StartMergeResponse("Failed to reset develop branch.");
                }
            }

            // Create out (or reset) the staging branch from develop
            var stagingBranchStatus =
                await git.NewBranch(stagingBranchName, startPoint: "develop", force: true, noTrack: true);
            if (!stagingBranchStatus.Success)
            {
                LogFailedCommand("Failed to create staging branch " + stagingBranchStatus, stagingBranchStatus);
                return new StartMergeResponse("Could not create branch " + stagingBranchName);
            }
            // Checkout the staging branch
            var stagingCheckoutStatus = await git.SetBranch(stagingBranchName);
            if (!stagingCheckoutStatus.Success)
            {
                LogFailedCommand("Failed to checkout staging branch " + stagingBranchName, stagingCheckoutStatus);
                return new StartMergeResponse("Could not checkout staging branch " + stagingBranchName);
            }

            // Get all commits on the edit branch that are not on the develop branch
            var logStatus = await git.Log("develop.." + editBranchName);
            if (!logStatus.Success)
            {
                LogFailedCommand("Failed to read commit log for edit branch " + editBranchName, logStatus);
                return new StartMergeResponse("Could not read commit log for branch " + editBranchName);
            }
            // Reverse the order so that the oldest commit is first
            logStatus.Commits.Reverse();

            // Apply the triple changes in each commit to the staging branch
            string prevCommitHash = null;
            foreach (var commit in logStatus.Commits)
            {
                var diffComparison = (prevCommitHash ?? (commit.Hash + "^")) + ".." + commit.Hash;
                var diffStatus = await git.Diff(diffComparison);
                if (!diffStatus.Success)
                {
                    LogFailedCommand("Failed to retrieve diff " + diffComparison, diffStatus);
                    return new StartMergeResponse("Could not retrieve diff.");
                }
                if (!ApplyDiff(diffStatus))
                {
                    return new StartMergeResponse("Failed to apply commit diffs");
                }
                var addStatus = await git.AddAll();
                if (!addStatus.Success)
                {
                    LogFailedCommand("Failed to stage changes", addStatus);
                    return new StartMergeResponse("Could not stage edits to git repository.");
                }
                var commitStatus = await git.Commit(reuseMessage: commit.Hash);
                if (!commitStatus.Success)
                {
                    LogFailedCommand("Failed to commit staged changes", commitStatus);
                    return new StartMergeResponse("Commit of staging update failed.");
                }
                prevCommitHash = commit.Hash;
            }

            return new StartMergeResponse(developUpdated);
        }

        public async Task<bool> CompleteMerge(string editBranchName, string stagingBranchName)
        {
            var git = _gitWrapperFactory.MakeGitWrapper(_repositoryPath);

            // Checkout develop branch
            var checkoutResult = await git.SetBranch("develop");
            if (!checkoutResult.Success)
            {
                LogFailedCommand("Failed to checkout develop branch", checkoutResult);
                return false;
            }

            // Merge staging into develop - ffonly
            var mergeResult = await git.Merge(stagingBranchName, noCommit: true, ffOnly: true);
            if (!mergeResult.Success)
            {
                LogFailedCommand("Failed to merge staging branch " + stagingBranchName + " into develop", mergeResult);
                return false;
            }

            // Push develop branch
            var pushResult = await git.Push("develop");
            if (!pushResult.Success)
            {
                LogFailedCommand("Failed to push updated develop branch", pushResult);
                return false;
            }

            // Delete staging branch
            var deleteResult = await git.DeleteBranch(stagingBranchName, true);
            if (!deleteResult.Success)
            {
                LogFailedCommand("Failed to delete staging branch " + stagingBranchName + " after merge completed", deleteResult);
            }

            // Delete edit branch
            deleteResult = await git.DeleteBranch(editBranchName, true);
            if (!deleteResult.Success)
            {
                LogFailedCommand("Failed to delete edit branch " + editBranchName + " after merge completed", deleteResult);
            }
            return true;
        }

        public async Task<QuinceDiff> Diff(string commit)
        {
            var git = _gitWrapperFactory.MakeGitWrapper(_repositoryPath);
            var gitDiff = await git.Diff(commit);
            if (!gitDiff.Success)
            {
                LogFailedCommand("Failed to retrieve diff for " + commit, gitDiff);
            }
            var quinceDiff = new QuinceDiff();
            ParseDiff(gitDiff, quinceDiff);
            return quinceDiff;
        }

        public void ParseDiff(IDiffResult diffResult, QuinceDiff quinceDiff)
        {
            // TODO: Make async
            Log.LogTrace("ParseDiff: {0}", diffResult.ToString());
            if (!diffResult.Success)
            {
                Log.LogTrace("ParseDiff: Aborting attempt to apply a failed diff command result");
                return;
            }
            var diffGraph = new NonIndexedGraph { BaseUri = new Uri("http://example.org/") };
            var parser = new NQuadsParser();
            Log.LogTrace("ParseDiff: Parsing deletes");
            parser.Parse(diffResult.FileDiffs.Where(fd=>fd.FilePath.StartsWith("_s")).SelectMany(diff => diff.Deleted),
                quinceDiff.Deleted,
                diffGraph);
            diffGraph.Clear();
            Log.LogTrace("ParseDiff: Parsing inserts");
            parser.Parse(diffResult.FileDiffs.Where(fd => fd.FilePath.StartsWith("_s")).SelectMany(diff => diff.Inserted),
                quinceDiff.Inserted,
                diffGraph);
        }

        private static void LogFailedCommand(string msg, ICommandResultWrapper cmdResultWrapper)
        {
            LogFailedCommand(msg, cmdResultWrapper.CommandResult);
        }

        private static void LogFailedCommand(string msg, CommandResult cmdResult)
        {
            Log.LogError("{0} Command exit code: {1}\nStdout: {2}\nStderr: {3}", msg, cmdResult.ExitCode,
                cmdResult.StandardOutput, cmdResult.StandardError);
        }
    }
}
