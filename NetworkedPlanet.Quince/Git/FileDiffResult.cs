using System.Collections.Generic;

namespace NetworkedPlanet.Quince.Git
{
    public class FileDiffResult : IFileDiffResult
    {
        public string FilePath { get; private set; }
        public List<string> Inserted { get; private set; }
        public List<string> Deleted { get; private set; }

        public FileDiffResult(string filePath)
        {
            FilePath = filePath;
            Inserted = new List<string>();
            Deleted = new List<string>();
        }
    }
}