using System;
using System.Diagnostics;
using System.IO;
using VDS.RDF;

namespace NetworkedPlanet.Quince.Import
{
    class Program
    {
        static int Main(string[] args)
        {
            Options options;
            try
            {
                var parser = new OptionsParser();
                options = parser.Parse(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }

            EnsureRepository(options.RepoDirectory);
            var repo = new DynamicFileStore(options.RepoDirectory, 1000);
            var graph = new Graph {BaseUri = options.GraphUri};
            graph.LoadFromFile(options.ImportFile);
            var sw = Stopwatch.StartNew();
            repo.Assert(graph);
            repo.Flush();
            sw.Stop();
            Console.WriteLine($"Imported {graph.Triples.Count} triples in {sw.ElapsedMilliseconds} ms");
            return 0;
        }

        private static void EnsureRepository(string repoDir)
        {
            if (!Directory.Exists(repoDir))
            {
                Directory.CreateDirectory(repoDir);
            }
        }
    }
}
