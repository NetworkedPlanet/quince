namespace NetworkedPlanet.Quince.Git
{
    /// <summary>
    /// Interface to be implemented by a factory class for <see cref="IGitWrapper"/> instances
    /// </summary>
    public interface IGitWrapperFactory
    {
        /// <summary>
        /// Create a new <see cref="IGitWrapper"/> instance
        /// </summary>
        /// <param name="workingDirectory">The working directory to use when executing Git commands</param>
        /// <param name="ceilingDirectory">OPTIONAL: An additional ceiling directory for the Git command-line. This can be added to minimize the search for a .git directory</param>
        /// <returns></returns>
        IGitWrapper MakeGitWrapper(string workingDirectory, string ceilingDirectory = null);
    }
}
