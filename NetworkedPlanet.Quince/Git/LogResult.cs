using System;
using System.Collections.Generic;
using Medallion.Shell;

namespace NetworkedPlanet.Quince.Git
{
    public class LogResult : CommandResultWrapper
    {
        public List<CommitInfo> Commits { get; }
        public LogResult(CommandResult commandResult) : base(commandResult)
        {
            Commits = new List<CommitInfo>();
            if (commandResult.Success)
            {
                Parse(commandResult.StandardOutput);
            }
        }

        private void Parse(string output)
        {
            var lines = output.Split('\n');
            CommitInfo current = null;
            foreach (var line in lines)
            {
                if (current != null)
                {
                    if (line.StartsWith("Author:"))
                    {
                        current.Author = line.Substring(8);
                    }
                    else if (line.StartsWith("Date: "))
                    {
                        current.Date = DateTime.Parse(line.Substring(6));
                    }
                    else if (line.StartsWith("    "))
                    {
                        if (current.Subject == null)
                        {
                            current.Subject = line.Substring(4);
                        }
                        else
                        {
                            if (current.Message == null)
                            {
                                current.Message = line.Substring(4);
                            }
                            else
                            {
                                current.Message += line.Substring(4);
                            }
                        }
                    }
                }
                if (line.StartsWith("Commit: "))
                {
                    if (current != null) Commits.Add(current);
                    current = new CommitInfo {Hash = line.Substring(8)};
                }
            }
            if (current != null) Commits.Add(current);
        }
    }

    public class CommitInfo
    {
        public string Hash { get; set; }
        public string Author { get; set; }
        public DateTime Date { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    } 
}
