namespace NetworkedPlanet.Quince.Repository
{
    public class BranchStatus
    {
        public string LocalBranchName { get; set; }
        public string OriginBranchName { get; set; }
        public int LocalCommits { get; set; }
        public int OriginCommits { get; set; }

        public BranchStatus(string localBranchName, string originBranchName, int localCommits, int originCommits)
        {
            LocalBranchName = localBranchName;
            OriginBranchName = originBranchName;
            LocalCommits = localCommits;
            OriginCommits = originCommits;
        }
    }
}