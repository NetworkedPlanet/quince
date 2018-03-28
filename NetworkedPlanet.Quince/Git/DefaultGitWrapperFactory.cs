using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkedPlanet.Quince.Git
{
    /// <summary>
    /// A default implementation of the <see cref="IGitWrapperFactory"/> interface that returns
    /// the standard <see cref="GitWrapper"/> implementation of the <see cref="IGitWrapper"/> interface.
    /// </summary>
    public class DefaultGitWrapperFactory : IGitWrapperFactory
    {
        private readonly string _gitPath;

        /// <summary>
        /// Create a new factory instance
        /// </summary>
        /// <param name="gitPath">The path to the Git executable to be used</param>
        public DefaultGitWrapperFactory(string gitPath)
        {
            _gitPath = gitPath;
        }

        /// <inheritdoc/>
        public IGitWrapper MakeGitWrapper(string workingDirectory, string ceilingDirectory = null)
        {
            return new GitWrapper(workingDirectory, _gitPath, ceilingDirectory);
        }
    }
}
