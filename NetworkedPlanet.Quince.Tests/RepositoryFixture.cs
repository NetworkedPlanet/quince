using System;
using System.IO;
using VDS.RDF;

namespace NetworkedPlanet.Quince.Tests
{
    public class RepositoryFixture : IDisposable
    {
        private readonly DirectoryInfo _baseDirectory;
        public DynamicFileStore Store { get; private set; }
        public DirectoryInfo BaseDirectory { get { return _baseDirectory; } } 

        public RepositoryFixture(string prefix)
        {
            _baseDirectory = Directory.CreateDirectory("tmp\\" + prefix + "-" + DateTime.UtcNow.Ticks);
            Store = new DynamicFileStore(_baseDirectory.FullName, 1000, 10);
        }

        public void Import(string testDataFile)
        {
            var defaultGraph = new Graph();
            var parser = new NQuadsParser();
            using (var reader = File.OpenText(testDataFile))
            {
                parser.Parse(reader, t=>Store.Assert(t.Subject, t.Predicate, t.Object, t.GraphUri), defaultGraph);
            }
            Store.Flush();
        }

        public void Dispose()
        {
            TestHelper.DeleteDirectory(_baseDirectory.FullName);
        }
    }
}