using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Medallion.Shell;
using Microsoft.Extensions.Logging;

namespace NetworkedPlanet.Quince.Git
{
    /// <summary>
    /// A standard implementation of the <see cref="IGitWrapper"/> interface that uses MedallionShell to execute Git commands
    /// </summary>
    public class GitWrapper : IGitWrapper
    {
        private readonly string _git;
        private readonly Shell _shell;
        private const string LogFormat = "Commit: %H%nAuthor: %aN%nDate: %aI%n%w(0,4,4)%s%n%-b";
        private static readonly ILogger Logger = QuinceLogging.CreateLogger<GitWrapper>();

        public GitWrapper(string workingDirectory = ".", string gitPath = "git", string ceilingDirectory=null)
        {
            _shell = new Shell(options =>
            {
                options.WorkingDirectory(Path.GetFullPath(workingDirectory));
                if (ceilingDirectory != null)
                {
                    options.StartInfo(x => x.EnvironmentVariables.Add("GIT_CEILING_DIRECTORIES", ceilingDirectory));
                }
            });
            _git = gitPath;
        }

        private Task<CommandResult> LogCommand(IReadOnlyCollection<string> args)
        {
            var execTask = _shell.Run(_git, args).Task;
            return execTask.ContinueWith(t =>
            {
                if (t.Result.Success)
                {
                    Logger.LogInformation("git {0}\n\tExit Code: {1}\n\tStdout: {2}\n\tStderr: {3}", string.Join(" ", args),
                        t.Result.ExitCode, t.Result.StandardOutput, t.Result.StandardError);
                }
                else
                {
                    Logger.LogError("git {0}\n\tExit Code: {1}\n\tStdout: {2}\n\tStderr: {3}", string.Join(" ", args),
                        t.Result.ExitCode, t.Result.StandardOutput, t.Result.StandardError);
                }
                return t.Result;
            });
        }

        public Task<CommandResult> Clone(string repository, string localDirectory, bool bare=false, string branch=null, int depth=0)
        {
            var args = new List<string> {"clone"};
            if (bare) args.Add("--bare");
            args.Add(repository);
            args.Add(localDirectory);
            if (branch != null)
            {
                args.Add("--branch");
                args.Add(branch);
            }
            if (depth > 0)
            {
                args.Add("--depth");
                args.Add(depth.ToString("D"));
            }
            return LogCommand(args);
        }

        public Task<DiffResult> Diff(string comparisonBranch)
        {
            return LogCommand(new [] {"diff", comparisonBranch}).ContinueWith(ParseDiff);
        }

        public Task<DiffResult> DiffHead()
        {
            return LogCommand(new [] {"diff", "HEAD"}).ContinueWith(ParseDiff);
        }

        public Task<CommandResult> AddAll()
        {
            return LogCommand(new[] {"add", "--all"});
        }

        public Task<StatusResult> Status()
        {
            return
                LogCommand(new[] {"status", "--porcelain"})
                    .ContinueWith(commandTask => new StatusResult(commandTask.Result));
        }

        public Task<CommandResult> NewBranch(string branchName, string startPoint = null, bool noTrack = false, bool setUpstream=false, bool force=false)
        {
            var args = new List<string> {"checkout", force? "-B": "-b", branchName };
            if (noTrack) args.Add("--no-track");
            if (setUpstream)
            {
                return LogCommand(args).ContinueWith(
                    checkoutTask => checkoutTask.Result.Success
                        ? LogCommand(new [] { "push", "--set-upstream", "origin", branchName}).Result
                        : checkoutTask.Result
                );
            }
            if (startPoint != null)
            {
                args.Add(startPoint);
            }
            return LogCommand(args);
        }

        public Task<CommandResult> SetBranch(string branchName)
        {
            return LogCommand(new[] {"checkout", branchName});
        }

        public Task<CommandResult> Reset(GitResetMode mode, string commit)
        {
            var args = new List<string> {"reset"};
            switch (mode)
            {
                case GitResetMode.Hard:
                    args.Add("--hard");
                    break;
                case GitResetMode.Keep:
                    args.Add("--keep");
                    break;
                case GitResetMode.Merge:
                    args.Add("--merge");
                    break;
                case GitResetMode.Mixed:
                    args.Add("--mixed");
                    break;
                case GitResetMode.MixedN:
                    args.Add("--mixed");
                    args.Add("-N");
                    break;
                case GitResetMode.Soft:
                    args.Add("--soft");
                    break;
            }
            args.Add(commit);
            return LogCommand(args);
        }

        public Task<CommandResult> Commit(string subject=null, string body=null, string author = null, string reuseMessage=null)
        {
            var args = new List<string> {"commit"};
            if (subject != null)
            {
                args.Add("-m");
                args.Add(subject);
                if (body != null)
                {
                    foreach (var line in body.Split('\n'))
                    {
                        args.Add("-m");
                        args.Add(line);
                    }
                }
            }
            else if (reuseMessage != null)
            {
                args.Add("-C");
                args.Add(reuseMessage);
            }
            else
            {
                args.Add("--no-edit");
                args.Add("--allow-empty-message");
            }
            if (author != null)
            {
                args.Add("--author");
                args.Add(author);
            }
            return LogCommand(args);
        }

        /// <summary>
        /// Performs a fetch
        /// </summary>
        /// <remarks>This is the equivalent of the git command: git fetch</remarks>
        /// <returns>A <see cref="FetchResult"/> wrapper that provides access to a list of the branches that are modified by the fetch</returns>
        public Task<FetchResult> Fetch()
        {
            var args = new List<string> {"fetch"};
            return LogCommand(args).ContinueWith(
                t => new FetchResult(t.Result));
        }

        public Task<LogResult> Log(string revisionRange)
        {
            return LogCommand(new[] {"log", "--pretty=" + LogFormat, revisionRange}).ContinueWith(
                t => new LogResult(t.Result));
        }

        /// <summary>
        /// Merges the named branch into the current branch without commiting the result
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="noCommit">Do not apply an auto commit to the result of the merge. This adds the --no-commit option to the git merge command</param>
        /// <param name="ffOnly">Fail if the merge cannot be resolved by a fast-forwards. This adds the --ff-only option to the git merge command</param>
        /// <returns></returns>
        public Task<CommandResult> Merge(string branch, bool noCommit=false, bool ffOnly=false)
        {
            var args = new List<string> { "merge" };
            if (noCommit) args.Add("--no-commit");
            if (ffOnly) args.Add("--ff-only");
            args.Add(branch);
            return LogCommand(args);
        }

        /// <summary>
        /// Performs a pull from origin and commits the resulting merge
        /// </summary>
        /// <returns></returns>
        /// <remarks>This is equivalent to the git command: git pull origin</remarks>
        public Task<CommandResult> Pull()
        {
            return LogCommand(new [] {"pull", "origin"});
        }

        /// <summary>
        /// Peforms a push to origin on the current branch
        /// </summary>
        /// <returns></returns>
        public Task<CommandResult> Push(string branchName = null)
        {
            var args = new List<string> {"push", "origin"};
            if (branchName != null) args.Add(branchName);
            return LogCommand(args);
        }

        public Task<CommandResult> PushTo(string repository, string branchName = null, bool setUpstream=false)
        {
            var args = new List<string> {"push"};
            if (setUpstream) args.Add("--set-upstream");
            args.Add(repository);
            if (branchName != null)
            {
                args.Add(branchName);
            }
            return LogCommand(args);
        }

        public Task<CommandResult> SetUserName(string userName, bool global = false)
        {
            var args = new List<string> {"config"};
            if (global) args.Add("--global");
            args.Add("user.name");
            args.Add(userName);
            return LogCommand(args);
        }

        public Task<CommandResult> SetUserEmail(string email, bool global = false)
        {
            var args = new List<string> { "config" };
            if (global) args.Add("--global");
            args.Add("user.email");
            args.Add(email);
            return LogCommand(args);
        }

        /// <summary>
        /// List branches, optionally filtering to match a pattern
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public Task<string[]> ListBranches(string pattern = null)
        {
            var branchTask = pattern != null
                ? LogCommand(new[] {"branch", "--list", pattern})
                : LogCommand(new[] {"branch", "--list"});
            return
                branchTask.ContinueWith(
                    t => t.Result.StandardOutput.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).Select(b=>b.TrimStart('*', ' ')).ToArray());
        }

        public Task<int> RevListCount(string commits)
        {
            return LogCommand(new[] {"rev-list", "--count", commits})
                .ContinueWith(revListTask =>
                {
                    if (revListTask.IsFaulted || !revListTask.Result.Success)
                    {
                        return -1;
                    }
                    return int.Parse(revListTask.Result.StandardOutput);
                });
        }

        public Task<CommandResult> DeleteBranch(string branchName, bool force = false)
        {
            var args = new List<string> { "branch", "--delete", "--quiet" };
            if (force) args.Add("--force");
            args.Add(branchName);
            return LogCommand(args);
        }

        /// <summary>
        /// Very basic ls-remote wrapper. Only supports the options needed to test for an empty repository on the remote
        /// </summary>
        /// <param name="repoUrl"></param>
        /// <param name="headsOnly"></param>
        /// <param name="setExitCode"></param>
        /// <returns></returns>
        public Task<CommandResult> ListRemote(string repoUrl, bool headsOnly = false, bool setExitCode = false)
        {
            var args = new List<string> {"ls-remote"};
            if (headsOnly) args.Add("-h");
            if (setExitCode) args.Add("--exit-code");
            args.Add(repoUrl);
            return LogCommand(args);
        }

        /// <summary>
        /// Very basic remote add wrapper. Only supports adding a named remote URL
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public Task<CommandResult> AddRemote(string name, string url)
        {
            var args = new List<string> {"remote", "add", name, url};
            return LogCommand(args);
        }

        public Task<CommandResult> Init()
        {
            var args = new List<string> {"init"};
            return LogCommand(args);
        }

        private DiffResult ParseDiff(Task<CommandResult> commandTask)
        {
            return new DiffResult(commandTask.Result);
        }

    }
}