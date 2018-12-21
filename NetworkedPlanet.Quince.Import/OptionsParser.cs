using System;
using System.IO;

namespace NetworkedPlanet.Quince.Import
{
    internal class OptionsParser
    {
        public Options Parse(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Unexpected number of arguments on the command line. Expected exactly 3 arguments.");
            }

            var opts = new Options {RepoDirectory = args[0], ImportFile = args[1]};
            if (!File.Exists(opts.ImportFile))
            {
                throw new ArgumentException($"Could not find the file '{opts.ImportFile}' for import.");
            }

            if (!Uri.TryCreate(args[2], UriKind.Absolute, out var graphUri))
            {
                throw new ArgumentException("Graph URI must be a valid absolute URI");
            }
            opts.GraphUri = graphUri;

            return opts;
        }
    }
}