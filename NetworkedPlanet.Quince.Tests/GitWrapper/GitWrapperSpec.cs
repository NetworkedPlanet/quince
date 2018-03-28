using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetworkedPlanet.Quince.Git;
using Xunit;

namespace NetworkedPlanet.Quince.Tests.GitWrapper
{
    [Collection("Basic Repository Collection")]
    [Trait("Category", "GitWrapper")]
    public class GitWrapperSpec : IDisposable
    {
        private readonly RepositoryFixture _fixture;
        private readonly string _testId;
        private int _repoId;
        private readonly List<string> _tempList;

        public GitWrapperSpec(RepositoryFixture fixture)
        {
            _testId = Path.Combine(fixture.TempDir, DateTime.Now.Ticks.ToString());
            _tempList = new List<string>();
            _fixture = fixture;
        }

        [Fact]
        public void EmptyRepositoryExists()
        {
            Assert.True(Directory.Exists(_fixture.EmptyRepositoryPath));
            Assert.True(Directory.Exists(Path.Combine(_fixture.EmptyRepositoryPath, ".git")));
        }

        [Fact]
        public void PopulatedRepositorExists()
        {
            Assert.True(Directory.Exists(_fixture.PopulatedRepositoryPath));
            Assert.True(Directory.Exists(Path.Combine(_fixture.PopulatedRepositoryPath, ".git")));
        }

        [Fact]
        public void CanCloneAnEmptyRepository()
        {
            var wrapper= new Git.GitWrapper();
            var cloneTask = wrapper.Clone(_fixture.EmptyRepositoryPath, _testId);
            var cloneResult = cloneTask.Result;
            Assert.True(cloneResult.Success);
            Assert.True(Directory.Exists(_testId));
            Assert.True(Directory.Exists(Path.Combine(_testId, ".git")));
            _tempList.Add(_testId);
        }

        [Fact]
        public void CanCloneAPopulatedRepository()
        {
            var wrapper = new Git.GitWrapper();
            var cloneTask = wrapper.Clone(_fixture.PopulatedRepositoryPath, _testId);
            var cloneResult = cloneTask.Result;
            Assert.True(cloneResult.Success);
            Assert.True(Directory.Exists(_testId));
            Assert.True(Directory.Exists(Path.Combine(_testId, ".git")));
            Assert.True(File.Exists(Path.Combine(_testId, "test.txt")));
            _tempList.Add(_testId);
        }

        [Fact]
        public async void GitWrapperFactoryReturnsProperlyConfiguredGitWrapper()
        {
            var wrapperFactory = new DefaultGitWrapperFactory("git");
            var cloneWrapper = wrapperFactory.MakeGitWrapper(".");
            var cloneResult = await cloneWrapper.Clone(_fixture.PopulatedRepositoryPath, _testId);
            Assert.True(cloneResult.Success);
            Assert.True(Directory.Exists(_testId));
            Assert.True(Directory.Exists(Path.Combine(_testId, ".git")));
            Assert.True(File.Exists(Path.Combine(_testId, "test.txt")));
            _tempList.Add(_testId);

            var repoWrapper = wrapperFactory.MakeGitWrapper(_testId);
            var statusResult = await repoWrapper.Status();
            Assert.True(statusResult.Success);
        }

        [Fact]
        public void CanCloneFromGithub()
        {
            var wrapper = new Git.GitWrapper();
            var cloneTask = wrapper.Clone("https://github.com/kal/semlove.git", _testId);
            var cloneResult = cloneTask.Result;
            Assert.True(cloneResult.Success);
            Assert.True(Directory.Exists(_testId));
            Assert.True(Directory.Exists(Path.Combine(_testId, ".git")));
            Assert.True(File.Exists(Path.Combine(_testId, "README.MD")));
        }

        [Fact]
        public void LocalDiffReportsLineInserted()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var testFile = Path.Combine(testPath, "test.txt");
            var lines = File.ReadAllLines(testFile).ToList();
            lines.Insert(1, "Inserted Line");
            File.WriteAllLines(testFile, lines);
            var wrapper = new Git.GitWrapper(testPath);
            var diffResult = wrapper.DiffHead().Result;
            Assert.NotNull(diffResult);
            Assert.NotNull(diffResult.CommandResult);
            Assert.True(diffResult.Success);
            Assert.Single(diffResult.FileDiffs);
            Assert.Single(diffResult.FileDiffs[0].Inserted);
            Assert.Empty(diffResult.FileDiffs[0].Deleted);
            Assert.Equal("Inserted Line", diffResult.FileDiffs[0].Inserted[0]);
        }

        [Fact]
        public void LocalDiffReportsLineDeleted()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var testFile = Path.Combine(testPath, "test.txt");
            var lines = File.ReadAllLines(testFile).ToList();
            lines.RemoveAt(1);
            File.WriteAllLines(testFile, lines);
            var wrapper = new Git.GitWrapper(testPath);
            var diffResult = wrapper.DiffHead().Result;
            Assert.NotNull(diffResult);
            Assert.NotNull(diffResult.CommandResult);
            Assert.True(diffResult.Success);
            Assert.Single(diffResult.FileDiffs);
            Assert.Single(diffResult.FileDiffs[0].Deleted);
            Assert.Empty(diffResult.FileDiffs[0].Inserted);
            Assert.Equal("Line 2", diffResult.FileDiffs[0].Deleted[0]);
        }

        [Fact]
        public void LocalDiffReportsFileDeleted()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var testFile = Path.Combine(testPath, "test.txt");
            File.Delete(testFile);
            var wrapper = new Git.GitWrapper(testPath);
            var addResult = wrapper.AddAll().Result;
            Assert.True(addResult.Success);
            var diffResult = wrapper.DiffHead().Result;
            Assert.NotNull(diffResult);
            Assert.NotNull(diffResult.CommandResult);
            Assert.True(diffResult.Success);
            Assert.Single(diffResult.FileDiffs);
            Assert.Equal(3, diffResult.FileDiffs[0].Deleted.Count);
            Assert.Empty(diffResult.FileDiffs[0].Inserted);
            Assert.Equal("Line 1", diffResult.FileDiffs[0].Deleted[0]);
            Assert.Equal("Line 2", diffResult.FileDiffs[0].Deleted[1]);
            Assert.Equal("Line 3", diffResult.FileDiffs[0].Deleted[2]);
        }

        [Fact]
        public void LocalDiffReportsNewFileAdded()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var newFile = Path.Combine(testPath, "new.txt");
            var lines = new[] {"Hello", "World"};
            File.WriteAllLines(newFile, lines);
            var wrapper = new Git.GitWrapper(testPath);
            var addResult = wrapper.AddAll().Result;
            Assert.True(addResult.Success);
            var diffResult = wrapper.DiffHead().Result;
            Assert.NotNull(diffResult);
            Assert.NotNull(diffResult.CommandResult);
            Assert.True(diffResult.Success);
            Assert.Single(diffResult.FileDiffs);
            Assert.Equal(2, diffResult.FileDiffs[0].Inserted.Count);
            Assert.Empty(diffResult.FileDiffs[0].Deleted);
            Assert.Equal("Hello", diffResult.FileDiffs[0].Inserted[0]);
            Assert.Equal("World", diffResult.FileDiffs[0].Inserted[1]);
        }

        [Fact]
        public void StatusReportsModifiedFile()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var testFile = Path.Combine(testPath, "test.txt");
            var lines = File.ReadAllLines(testFile).ToList();
            lines.RemoveAt(1);
            File.WriteAllLines(testFile, lines);
            var wrapper = new Git.GitWrapper(testPath);
            Assert.True(wrapper.AddAll().Result.Success);
            var statusResult = wrapper.Status().Result;
            Assert.True(statusResult.Success);
            Assert.Single(statusResult.ModifiedFiles);
            Assert.Equal("test.txt", statusResult.ModifiedFiles[0]);
        }

        [Fact]
        public void StatusReportsDeletedFile()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var testFile = Path.Combine(testPath, "test.txt");
            File.Delete(testFile);
            var wrapper = new Git.GitWrapper(testPath);
            Assert.True(wrapper.AddAll().Result.Success);
            var statusResult = wrapper.Status().Result;
            Assert.True(statusResult.Success);
            Assert.Single(statusResult.DeletedFiles);
            Assert.Equal("test.txt", statusResult.DeletedFiles[0]);
        }

        [Fact]
        public void StatusReportsNewFile()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var testFile = Path.Combine(testPath, "new.txt");
            File.WriteAllLines(testFile, new [] {"Hello", "World"});
            var wrapper = new Git.GitWrapper(testPath);
            Assert.True(wrapper.AddAll().Result.Success);
            var statusResult = wrapper.Status().Result;
            Assert.True(statusResult.Success);
            Assert.Single(statusResult.NewFiles);
            Assert.Equal("new.txt", statusResult.NewFiles[0]);
        }

        [Fact]
        public void CanCreateNewBranch()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var wrapper = new Git.GitWrapper(testPath);
            var branchResult = wrapper.NewBranch("test-branch").Result;
            Assert.True(branchResult.Success);
        }

        [Fact]
        public void CanDeleteBranch()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var wrapper = new Git.GitWrapper(testPath);
            var createBranchResult = wrapper.NewBranch("test-branch").Result;
            Assert.True(createBranchResult.Success);
            var checkoutResult = wrapper.SetBranch("master").Result;
            Assert.True(checkoutResult.Success);
            var deleteBranchResult = wrapper.DeleteBranch("test-branch").Result;
            Assert.True(deleteBranchResult.Success);
        }

        [Fact]
        public void CannotDeleteTheBranchYouAreOn()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var wrapper = new Git.GitWrapper(testPath);
            var createBranchResult = wrapper.NewBranch("test-branch").Result;
            Assert.True(createBranchResult.Success);
            var deleteBranchResult = wrapper.DeleteBranch("test-branch").Result;
            Assert.False(deleteBranchResult.Success);
        }

        [Fact]
        public void CanForceDeleteTheBranchYouAreOn()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var wrapper = new Git.GitWrapper(testPath);
            var createBranchResult = wrapper.NewBranch("test-branch").Result;
            Assert.True(createBranchResult.Success);
            var deleteBranchResult = wrapper.DeleteBranch("test-branch", force:true).Result;
            Assert.False(deleteBranchResult.Success);
        }

        [Fact]
        public void CannotCreateDuplicateBranch()
        {
            var testPath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var wrapper= new Git.GitWrapper(testPath);
            var branchResult = wrapper.NewBranch("test-branch").Result;
            Assert.True(branchResult.Success);
            var secondBranchResult = wrapper.NewBranch("test-branch").Result;
            Assert.False(secondBranchResult.Success);
        }

        [Fact]
        public void CanCommitWithDefaultAuthor()
        {
            var testPath = CloneTestRepository(_fixture.EmptyRepositoryPath);
            File.WriteAllLines(Path.Combine(testPath, "new.text"), new []{"Hello", "World"});
            var wrapper= new Git.GitWrapper(testPath);
            Assert.True(wrapper.AddAll().Result.Success);
            var commitResult = wrapper.Commit("First commit").Result;
            Assert.True(commitResult.Success);
        }

        [Fact]
        public void CanCommitWithOverrideAuthor()
        {
            var testPath = CloneTestRepository(_fixture.EmptyRepositoryPath);
            File.WriteAllLines(Path.Combine(testPath, "new.text"), new[] { "Hello", "World" });
            var wrapper = new Git.GitWrapper(testPath);
            Assert.True(wrapper.AddAll().Result.Success);
            var commitResult = wrapper.Commit("First commit", "Alice Example <alice@example.com>").Result;
            Assert.True(commitResult.Success);
        }

        [Fact]
        public void CanFetchFromRemoteRepository()
        {
            var remotePath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var localPath = CloneTestRepository(remotePath);
            File.WriteAllLines(Path.Combine(remotePath, "new.txt"), new [] {"Hello", "World"});
            var remoteWrapper = new Git.GitWrapper(remotePath);
            var localWrapper = new Git.GitWrapper(localPath);
            Assert.True(remoteWrapper.AddAll().Result.Success);
            Assert.True(remoteWrapper.Commit("Added a file").Result.Success);
            var fetchResult = localWrapper.Fetch().Result;
            Assert.True(fetchResult.Success);
            Assert.Contains("master", fetchResult.UpdatedBranches);
        }

        [Fact]
        public void CanMergeFetchHeadFastForwards()
        {
            var remotePath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var localPath = CloneTestRepository(remotePath);
            File.WriteAllLines(Path.Combine(remotePath, "new.txt"), new[] { "Hello", "World" });
            var remoteWrapper = new Git.GitWrapper(remotePath);
            var localWrapper = new Git.GitWrapper(localPath);
            Assert.True(remoteWrapper.AddAll().Result.Success);
            Assert.True(remoteWrapper.Commit("Added a file").Result.Success);
            var fetchResult = localWrapper.Fetch().Result;
            Assert.True(fetchResult.Success);
            Assert.Contains("master", fetchResult.UpdatedBranches);
            Assert.True(localWrapper.Merge("FETCH_HEAD", ffOnly: true).Result.Success);
        }

        [Fact]
        public void CanPullFromRemoteRepository()
        {
            var remotePath = CloneTestRepository(_fixture.PopulatedRepositoryPath);
            var localPath = CloneTestRepository(remotePath);
            File.WriteAllLines(Path.Combine(remotePath, "new.txt"), new [] {"Hello", "World"});
            var remoteWrapper = new Git.GitWrapper(remotePath);
            var localWrapper = new Git.GitWrapper(localPath);
            Assert.True(remoteWrapper.AddAll().Result.Success);
            Assert.True(remoteWrapper.Commit("Added a file").Result.Success);
            Assert.True(localWrapper.Pull().Result.Success);
            Assert.True(File.Exists(Path.Combine(localPath, "new.txt")));
        }

        [Fact]
        public void CanPushToRemoteRepository()
        {
            var remotePath = CloneTestRepository(_fixture.PopulatedRepositoryPath, true);
            var localPath = CloneTestRepository(remotePath);
            var localWrapper = new Git.GitWrapper(localPath);
            var checkoutResult = localWrapper.SetBranch("master").Result;
            Assert.True(checkoutResult.Success);
            File.WriteAllLines(Path.Combine(localPath, "new.txt"), new[] { "Hello", "World" });
            Assert.True(localWrapper.AddAll().Result.Success);
            Assert.True(localWrapper.Commit("Added a file").Result.Success);
            var pushResult = localWrapper.Push().Result;
            Assert.True(pushResult.Success);

            // We can't test for the file existing in the remote repo because it is cloned as a bare repo.
            // So instead make a second clone and check that this gets the file that was pushed from the first one
            var newLocalPath = CloneTestRepository(remotePath);
            Assert.True(File.Exists(Path.Combine(newLocalPath, "new.txt")));
        }


        private string CloneTestRepository(string sourceRepositoryPath, bool bare = false)
        {
            var wrapper = new Git.GitWrapper();
            var targetId = _testId + "." + _repoId++;
            var cloneTask = wrapper.Clone(sourceRepositoryPath, targetId, bare);
            Assert.True(cloneTask.Result.Success);
            _tempList.Add(targetId);
            return targetId;
        }

        public void Dispose()
        {
            foreach (var dir in _tempList)
            {
                TestHelper.DeleteDirectory(dir);
            }
        }
    }

}
