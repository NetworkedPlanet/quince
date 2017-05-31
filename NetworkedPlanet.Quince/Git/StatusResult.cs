using System.Collections.Generic;
using System.IO;
using Medallion.Shell;

namespace NetworkedPlanet.Quince.Git
{
    public class StatusResult : CommandResultWrapper
    {
        public List<string> ModifiedFiles { get; private set; }
        public List<string> NewFiles { get; private set; }
        public List<string> DeletedFiles { get; private set; }

        public StatusResult(CommandResult commandResult):base(commandResult)
        {
            ModifiedFiles = new List<string>();
            NewFiles= new List<string>();
            DeletedFiles = new List<string>();
            if (commandResult.Success)
            {
                ParseStatusResult(commandResult.StandardOutput);
            }
        }

        private void ParseStatusResult(string stdout)
        {
            using (var reader = new StringReader(stdout))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("M"))
                    {
                        ModifiedFiles.Add(line.Substring(2).TrimStart());
                    }
                    if (line.StartsWith("D"))
                    {
                        DeletedFiles.Add(line.Substring(2).TrimStart());
                    }
                    if (line.StartsWith("A"))
                    {
                        NewFiles.Add(line.Substring(2).TrimStart());
                    }
                    // renames (R) are ignored - won't be used in a Quince repository
                }
            }
        }
    }
}
