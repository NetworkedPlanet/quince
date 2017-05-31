using System;
using System.IO;
using Medallion.Shell;
using Xunit;

namespace NetworkedPlanet.Quince.Tests.GitWrapper
{
    [CollectionDefinition("Basic Repository Collection")]
    public class BasicRepositoryCollection : ICollectionFixture<RepositoryFixture> { }

    public class RepositoryFixture : IDisposable
    {
        private string _fixtureId;
        public string TempDir = "_tmp";
        public string EmptyRepositoryPath;
        public string PopulatedRepositoryPath;

        public RepositoryFixture()
        {
            _fixtureId = Path.Combine(DateTime.Now.Ticks.ToString());
            InitialiseEmptyRepository();
            InitialisePopulatedRepository();
        }

        private void InitialiseEmptyRepository()
        {
            EmptyRepositoryPath = Path.Combine(TempDir, "empty_" + _fixtureId);
            CreateEmptyRepository(EmptyRepositoryPath);
        }

        private void CreateEmptyRepository(string repoPath)
        {
            Directory.CreateDirectory(repoPath);
            var taskResult = Command.Run("git", "init", repoPath).Result;
            if (!taskResult.Success)
                throw new Exception("Setup failed - could not initialise test empty repo at " + repoPath);
        }

        private void InitialisePopulatedRepository()
        {
            PopulatedRepositoryPath = Path.Combine(TempDir, "populated_" + _fixtureId);
            CreateEmptyRepository(PopulatedRepositoryPath);
            var sourceFile = Path.Combine(PopulatedRepositoryPath, "test.txt");
            using (var writer = File.CreateText(sourceFile))
            {
                writer.WriteLine("Line 1");
                writer.WriteLine("Line 2");
                writer.WriteLine("Line 3");
            }
            var addResult = Command.Run("git", new[] {"add", "--all"}, options=>options.WorkingDirectory(PopulatedRepositoryPath)).Result;
            if (!addResult.Success)
            {
                throw new Exception("Setup failed - could not add files to repo at "  +PopulatedRepositoryPath);
            }
            var taskResult = Command.Run("git", new[] {"commit", "-m", "Intial commit message"},
                options => options.WorkingDirectory(PopulatedRepositoryPath)).Result;
            if (!taskResult.Success)
            {
                throw new Exception("Setup failed - could not commit changes to repo at " + PopulatedRepositoryPath + "\n\tOutput:\n" + taskResult.StandardOutput + "\n\tError:" + taskResult.StandardError );
            }
        }

        public void Dispose()
        {
            TestHelper.DeleteDirectory(EmptyRepositoryPath);
            TestHelper.DeleteDirectory(PopulatedRepositoryPath);
        }

    }
}