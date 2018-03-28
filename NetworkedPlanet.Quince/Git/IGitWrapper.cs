using System.Threading.Tasks;
using Medallion.Shell;

namespace NetworkedPlanet.Quince.Git
{
    /// <summary>
    /// Interface to be implemented by classes that provide a wrapper around Git commands
    /// </summary>
    public interface IGitWrapper
    {
        Task<CommandResult> AddAll();
        Task<CommandResult> AddRemote(string name, string url);
        Task<CommandResult> Clone(string repository, string localDirectory, bool bare = false, string branch = null, int depth = 0);
        Task<CommandResult> Commit(string subject = null, string body = null, string author = null, string reuseMessage = null);
        Task<CommandResult> DeleteBranch(string branchName, bool force = false);
        Task<DiffResult> Diff(string comparisonBranch);
        Task<DiffResult> DiffHead();
        Task<FetchResult> Fetch();
        Task<CommandResult> Init();
        Task<string[]> ListBranches(string pattern = null);
        Task<CommandResult> ListRemote(string repoUrl, bool headsOnly = false, bool setExitCode = false);
        Task<LogResult> Log(string revisionRange);
        Task<CommandResult> Merge(string branch, bool noCommit = false, bool ffOnly = false);
        Task<CommandResult> NewBranch(string branchName, string startPoint = null, bool noTrack = false, bool setUpstream = false, bool force = false);
        Task<CommandResult> Pull();
        Task<CommandResult> Push(string branchName = null);
        Task<CommandResult> PushTo(string repository, string branchName = null, bool setUpstream = false);
        Task<CommandResult> Reset(GitResetMode mode, string commit);
        Task<int> RevListCount(string commits);
        Task<CommandResult> SetBranch(string branchName);
        Task<CommandResult> SetUserEmail(string email, bool global = false);
        Task<CommandResult> SetUserName(string userName, bool global = false);
        Task<StatusResult> Status();
    }
}