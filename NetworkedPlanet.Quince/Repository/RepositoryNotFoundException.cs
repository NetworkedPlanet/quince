namespace NetworkedPlanet.Quince.Repository
{
    public sealed class RepositoryNotFoundException : QuinceException
    {
        public RepositoryNotFoundException(string repoPath):base("Could not find a Quince repository at " + repoPath) { }
    }
}