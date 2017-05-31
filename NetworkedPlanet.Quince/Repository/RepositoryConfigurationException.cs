using System;

namespace NetworkedPlanet.Quince.Repository
{
    public class RepositoryConfigurationException : QuinceException
    {
        public RepositoryConfigurationException(string msg) : base(msg)
        {
        }

        public RepositoryConfigurationException(string msg, Exception innerException) : base(msg, innerException)
        {
        }
    }
}