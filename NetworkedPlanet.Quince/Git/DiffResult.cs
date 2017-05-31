using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Medallion.Shell;

namespace NetworkedPlanet.Quince.Git
{
    public class DiffResult:CommandResultWrapper, IDiffResult
    {
        public List<IFileDiffResult> FileDiffs { get; }

        public DiffResult(CommandResult cmdResult):base(cmdResult)
        {
            if (cmdResult.Success)
            {
                FileDiffs = ParseDiffResults(cmdResult.StandardOutput).ToList();
            }
        }

        public static IEnumerable<IFileDiffResult> ParseDiffResults(string stdout)
        {
            FileDiffResult currentResult = null;
            using (var reader = new StringReader(stdout))
            {
                string line, left, right = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("diff"))
                    {
                        // Starting a new diff block
                        if (currentResult != null) yield return currentResult;
                        currentResult = null;
                        var tokens = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        left = tokens[2].Substring(2);
                        right = tokens[3].Substring(2);
                    }
                    if (line.StartsWith("@"))
                    {
                        // Skipped header lines
                        currentResult = new FileDiffResult(right);
                    }
                    if (currentResult != null && line.StartsWith("-"))
                    {
                        currentResult.Deleted.Add(line.TrimStart('-'));
                    }
                    if (currentResult != null && line.StartsWith("+"))
                    {
                        currentResult.Inserted.Add(line.TrimStart('+'));
                    }
                }
                if (currentResult != null) yield return currentResult;
            }
        }

       
    }

}