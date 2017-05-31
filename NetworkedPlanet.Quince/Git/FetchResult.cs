using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Medallion.Shell;

namespace NetworkedPlanet.Quince.Git
{
    public class FetchResult:CommandResultWrapper
    {
        public List<string> UpdatedBranches { get; }

        private readonly Regex _branchRegex = new Regex(@"(\S+)\s+->\s+");

        public FetchResult(CommandResult commandResult):base(commandResult)
        {
            if (commandResult.Success)
            {
                UpdatedBranches = ParseOutput(commandResult.StandardOutput).ToList();
                UpdatedBranches.AddRange(ParseOutput(commandResult.StandardError));
            }
        }

        private IEnumerable<string> ParseOutput(string stdout)
        {
            foreach (var line in stdout.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var lineMatch = _branchRegex.Match(line);
                if (lineMatch != null)
                {
                    yield return lineMatch.Groups[1].Value;
                }
            }
        }
    }
}
