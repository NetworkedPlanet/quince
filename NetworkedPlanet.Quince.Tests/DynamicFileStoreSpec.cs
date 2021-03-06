﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using VDS.RDF;
using Xunit;

namespace NetworkedPlanet.Quince.Tests
{
    public class DynamicFileStoreSpec
    {
        [Fact]
        public void CanDropAGraph()
        {
            using (var repoFixture = new RepositoryFixture("test-drop"))
            {
                repoFixture.Import("data\\test1.nq");
                var g = new Graph();
                var testUri0 = g.CreateUriNode(new Uri("http://example.org/s/0"));
                var testUri1 = g.CreateUriNode(new Uri("http://example.org/s/1"));
                var testUri2 = g.CreateUriNode(new Uri("http://example.org/s/2"));
                var testUri3 = g.CreateUriNode(new Uri("http://example.org/s/3"));
                var testUri4 = g.CreateUriNode(new Uri("http://example.org/s/4"));
                var testUri190 = g.CreateUriNode(new Uri("http://example.org/s/190"));
                repoFixture.Store.GetTriplesForSubject(testUri0).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri1).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri2).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri3).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri4).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri190).Should().HaveCount(5);

                repoFixture.Store.DropGraph(new Uri("http://example.org/g/1"));
                repoFixture.Store.Flush();

                repoFixture.Store.GetTriplesForSubject(testUri0).Should().HaveCount(0);
                repoFixture.Store.GetTriplesForSubject(testUri1).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri2).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri3).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri4).Should().HaveCount(5);
                repoFixture.Store.GetTriplesForSubject(testUri190).Should().HaveCount(0);
            }
        }

        [Fact]
        public void CanEnumerateSubjects()
        {
            using (var repoFixture = new RepositoryFixture("test-enumerate"))
            {
                repoFixture.Import("data\\test1.nq");
                var testHandler = new TestTripleCollectionHandler(tc =>
                {
                    tc.Count.Should().Be(5);
                    tc.All(x => x.Subject.Equals(tc[0].Subject)).Should().BeTrue();
                });
                repoFixture.Store.EnumerateSubjects(testHandler);
                testHandler.TotalCount.Should().Be(200);
            }
        }

        [Fact]
        public void CanGetTriplesWithSpecificSubject()
        {
            using (var repoFixture = new RepositoryFixture("test-getbysubject"))
            {
                repoFixture.Import("data\\test1.nq");
                var target = new Uri("http://example.org/s/0");
                var triples = repoFixture.Store.GetTriplesForSubject(target).ToList();
                triples.Count.Should().Be(5);
                triples.All(t => (t.Subject as IUriNode)?.Uri.Equals(target) ?? false).Should().BeTrue();
            }
        }

        [Fact]
        public void ItShouldReturnAnEmptyEnumerationWhenTheSpecifiedSubjectDoesNotExist()
        {
            using (var repoFixture = new RepositoryFixture("test-getbyinvalidsubject"))
            {
                repoFixture.Import("data\\test1.nq");
                var target = new Uri("http://example.org/o/0");
                var triples = repoFixture.Store.GetTriplesForSubject(target);
                triples.Should().BeEmpty();
            }
        }

        [Fact]
        public void CanGetTriplesWithSpecificObject()
        {
            using (var repoFixture = new RepositoryFixture("test-getbyobject"))
            {
                repoFixture.Import("data\\test1.nq");
                var target = new Uri("http://example.org/o/0");
                var triples = repoFixture.Store.GetTriplesForObject(target).ToList();
                triples.Count.Should().Be(5);
                triples.All(t => (t.Object as IUriNode)?.Uri.Equals(target) ?? false).Should().BeTrue();
            }
        }

        [Fact]
        public void ItReturnsAnEmptyEnumerationWhenTheSpecifiedObjectDoesNotExist()
        {
            using (var repoFixture = new RepositoryFixture("test-getbyinvalidobject"))
            {
                repoFixture.Import("data\\test1.nq");
                var target = new Uri("http://example.org/s/0");
                var triples = repoFixture.Store.GetTriplesForObject(target).ToList();
                triples.Should().BeEmpty();
            }

        }

        [Fact]
        public void CreatesDirectoryMap()
        {
            using (var repoFixture = new RepositoryFixture("test-enumerate"))
            {
                repoFixture.Import("data\\test1.nq");
                var dirMapPath = Path.Combine(repoFixture.BaseDirectory.FullName, "dirmap.txt");
                Assert.True(File.Exists(dirMapPath));
                var dirMapLines = File.ReadAllLines(dirMapPath);
                Assert.Contains(dirMapLines, x =>x.Equals("_s"));
                Assert.Contains(dirMapLines, x =>x.Equals("_o"));
                Assert.Contains(dirMapLines, x =>x.Equals("_p"));
                Assert.True(dirMapLines.All(x=>x.StartsWith("_s") || x.StartsWith("_o") || x.StartsWith("_p")));
            }
        }

        [Fact]
        public void CanEnumerateSubjectsSmall()
        {
            using (var repoFixture = new RepositoryFixture("test-enumerate"))
            {
                repoFixture.Import("data\\test2.nq");
                var testHandler = new TestTripleCollectionHandler(tc =>
                {
                    tc.Count.Should().Be(5);
                    tc.All(x => x.Subject.Equals(tc[0].Subject)).Should().BeTrue();
                });
                repoFixture.Store.EnumerateSubjects(testHandler);
                testHandler.TotalCount.Should().Be(1);
            }
        }

        [Fact]
        public void CanEnumerateSubjectStatements()
        {
            using (var repoFixture = new RepositoryFixture("test-enumerate"))
            {
                repoFixture.Import("data\\test3.nq");
                var testHandler = new TestResourceStatementHandler((subject, subjectStatements, objectStatements) =>
                {
                    subjectStatements.All(x => x.Subject.Equals(subject)).Should().BeTrue();
                    objectStatements.All(x => x.Object.Equals(subject)).Should().BeTrue();
                });
                repoFixture.Store.EnumerateSubjects(testHandler);
                testHandler.TotalSubjects.Should().Be(2);
                testHandler.TotalSubjectStatements.Should().Be(7);
                testHandler.TotalObjectStatements.Should().Be(2);
            }
        }

        [Fact]
        public void CanEnumerateObjectStatements()
        {
            using (var repoFixture = new RepositoryFixture("test-enumerate"))
            {
                repoFixture.Import("data\\test3.nq");
                var testHandler = new TestResourceStatementHandler((objectNode, subjectStatements, objectStatements) =>
                    {
                        subjectStatements.All(x => x.Subject.Equals(objectNode)).Should().BeTrue();
                        objectStatements.All(x => x.Object.Equals(objectNode)).Should().BeTrue();
                    });
                repoFixture.Store.EnumerateObjects(testHandler);
                testHandler.TotalSubjects.Should().Be(2);
                testHandler.TotalSubjectStatements.Should().Be(5);
                testHandler.TotalObjectStatements.Should().Be(7);
            }
        }

        [Fact]
        public void CanEnumerateObjectsSmall()
        {
            using (var repoFixture = new RepositoryFixture("test-enumerate"))
            {
                repoFixture.Import("data\\test2.nq");
                var testHandler = new TestTripleCollectionHandler(tc =>
                {
                    tc.Count.Should().Be(5);
                    tc.All(x => x.Object.Equals(tc[0].Object)).Should().BeTrue();
                });
                repoFixture.Store.EnumerateSubjects(testHandler);
                testHandler.TotalCount.Should().Be(1);
            }
        }

        [Fact]
        public void CanEnumerateSharedObjects()
        {
            using (var repoFixture = new RepositoryFixture("test-enumerate"))
            {
                repoFixture.Import("data\\test2a.nq");
                var testHandler = new TestTripleCollectionHandler(tc =>
                {
                    tc.All(x=>x.Object.Equals(tc[0].Object)).Should().BeTrue();
                    var objectUri = (tc[0].Object as IUriNode)?.Uri?.ToString();
                    objectUri.Should().NotBeNull();
                    if (objectUri.Equals("http://example.org/o/0"))
                    {
                        tc.Count.Should().Be(3);
                    }
                    if (objectUri.Equals("http://example.org/o/1"))
                    {
                        tc.Count.Should().Be(2);
                    }
                });
                repoFixture.Store.EnumerateObjects(testHandler);
                testHandler.TotalCount.Should().Be(2);
            }
        }
    }

    public class TestTripleCollectionHandler : ITripleCollectionHandler
    {
        public int TotalCount { get; private set; }
        private readonly Action<IList<Triple>> _validationAction;

        public TestTripleCollectionHandler(Action<IList<Triple>> validationAction)
        {
            _validationAction = validationAction;
        }
        public bool HandleTripleCollection(IList<Triple> tripleCollection)
        {
            _validationAction(tripleCollection);
            TotalCount++;
            return true;
        }
    }

    public class TestResourceStatementHandler : IResourceStatementHandler
    {
        public int TotalSubjects;
        public int TotalSubjectStatements;
        public int TotalObjectStatements;

        private readonly Action<INode, IList<Triple>, IList<Triple>> _validationAction;

        public TestResourceStatementHandler(Action<INode, IList<Triple>, IList<Triple>> validationAction)
        {
            _validationAction = validationAction;
        }

        public bool HandleResource(INode subject, IList<Triple> subjectStatements,
            IList<Triple> objectStatements)
        {
            _validationAction(subject, subjectStatements, objectStatements);
            TotalSubjects++;
            TotalSubjectStatements += subjectStatements.Count;
            TotalObjectStatements += objectStatements.Count;
            return true;
        }
    }
}
