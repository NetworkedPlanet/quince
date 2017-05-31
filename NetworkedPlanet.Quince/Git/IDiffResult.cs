using System.Collections.Generic;

namespace NetworkedPlanet.Quince.Git
{
    public interface IDiffResult : ICommandResultWrapper
    {
        List<IFileDiffResult> FileDiffs { get; }
    }
}