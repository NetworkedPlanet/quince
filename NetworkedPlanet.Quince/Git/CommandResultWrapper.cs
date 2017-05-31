using Medallion.Shell;

namespace NetworkedPlanet.Quince.Git
{
    public abstract class CommandResultWrapper : ICommandResultWrapper
    {
        public CommandResult CommandResult { get; }
        public bool Success => CommandResult.Success;

        protected CommandResultWrapper(CommandResult commandResult)
        {
            CommandResult = commandResult;
        }
    }
}
