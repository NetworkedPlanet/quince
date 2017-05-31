using System.Collections.Generic;

namespace NetworkedPlanet.Quince.Git
{
    public interface IFileDiffResult
    {
        string FilePath { get; }
        List<string> Inserted { get; }
        List<string> Deleted { get; }
    }
}