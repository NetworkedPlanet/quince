using Medallion.Shell;

namespace NetworkedPlanet.Quince.Git
{
    public interface ICommandResultWrapper
    {
        CommandResult CommandResult { get; }
        bool Success { get; }
    }
}