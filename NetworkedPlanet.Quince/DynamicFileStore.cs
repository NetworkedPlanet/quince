using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VDS.Common.Tries;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace NetworkedPlanet.Quince
{
    public class DynamicFileStore : IQuinceStore
    {
        private const string FileNameExtension = ".nq";
        private const string DirMapFileName = "dirmap.txt";
        private readonly int _cacheThreshold;
        private readonly int _splitThreshold;
        private readonly Dictionary<string, List<string>> _cache;

        private readonly DirectoryInfo _baseDirectory;
        private readonly Trie<string, string, string> _directoryMap;
        private readonly BaseFormatter _lineFormat;
        private readonly IStoreReader _lineReader;
        private readonly MD5 _hasher;

        private static readonly ILogger Log = QuinceLogging.CreateLogger<DynamicFileStore>();


        public DynamicFileStore(string baseDirectory, int cacheThreshold, int splitThreshold = 2048)
        {
            _cacheThreshold = cacheThreshold;
            _splitThreshold = splitThreshold;
            _cache = new Dictionary<string, List<string>>(_cacheThreshold);
            _baseDirectory = new DirectoryInfo(baseDirectory);
            if (!_baseDirectory.Exists)
            {
                Log.LogError("Could not find dynamic file store base directory at {0}", baseDirectory);
                throw new FileNotFoundException($"Could not find directory at {baseDirectory}");
            }
            _directoryMap = BuildDirectoryMap(_baseDirectory);
            _lineFormat = new NQuads11Formatter();
            _lineReader = new VDS.RDF.Parsing.NQuadsParser(NQuadsSyntax.Rdf11);
            _hasher = MD5.Create();
        }

        public void Assert(INode subject, INode predicate, INode obj, Uri graph)
        {
            Log.LogTrace("Assert: {0} {1} {2} <{3}>", subject, predicate, obj, graph);
            var subjectFileName = GetSubjectFileName(subject);
            var subjectPath = GetFilePath(subjectFileName);
            var lineStr = FormatTriple(subject, predicate, obj, graph);
            if (Assert(TripleSegment.Subject, lineStr, subjectPath))
            {
                var objectFileName = GetObjectFileName(obj);
                var objectPath = GetFilePath(objectFileName);
                Assert(TripleSegment.Object, lineStr, objectPath);
                var predicateFileName = GetPredicateFileName(predicate);
                var predicatePath = GetFilePath(predicateFileName);
                Assert(TripleSegment.Predicate, lineStr, predicatePath);
            }
        }

        public void Assert(IGraph graph)
        {
            Log.LogTrace($"AssertGraph: {graph.BaseUri}");
            foreach (var t in graph.Triples)
            {
                Assert(t.Subject, t.Predicate, t.Object, graph.BaseUri);
            }
        }

        public void Retract(INode subject, INode predicate, INode obj, Uri graph)
        {
            Log.LogTrace("Retract: {0} {1} {2} <{3}>",
                subject == null ? "?" : subject.ToString(),
                predicate == null ? "?" : predicate.ToString(),
                obj == null ? "?" : obj.ToString(),
                graph == null ? "?" : graph.ToString());
            if (subject == null || subject is IVariableNode)
            {
                if (obj == null || obj is IVariableNode)
                {
                    throw new ArgumentException("Retract requires either subject or object to be specified");
                }
                RetractByObject(subject, predicate, obj, graph);
            }
            else
            {
                RetractBySubject(subject, predicate, obj, graph);
            }
        }

        /// <summary>
        /// Drop all triples in the specified graph from the repository
        /// </summary>
        /// <param name="graph"></param>
        /// <remarks>NOTE: The current implementation will flush any pending changes to disk before
        /// processing the graph drop.</remarks>
        public void DropGraph(Uri graph)
        {
            Flush();
            var lineEnd = $"<{_lineFormat.FormatUri(graph)}>.";
            IterateFiles((file) => DropGraph(file, lineEnd));
        }

        private void IterateFiles(Action<FileInfo> filePathAction)
        {
            IterateFiles(_baseDirectory, filePathAction);
        }

        private static void IterateFiles(DirectoryInfo directory, Action<FileInfo> filePathAction)
        {
            foreach (var fileInfo in directory.GetFiles()) filePathAction(fileInfo);
            foreach (var subDir in directory.GetDirectories())
            {
                IterateFiles(subDir, filePathAction);
            }
        }

        private void DropGraph(FileInfo triplesFileInfo, string lineEnd)
        {
            var lines = new List<string>(File.ReadAllLines(triplesFileInfo.FullName, Encoding.UTF8));
            int linesRemoved = lines.RemoveAll(x => x.EndsWith(lineEnd));
            if (linesRemoved > 0) File.WriteAllLines(triplesFileInfo.FullName, lines, Encoding.UTF8);
        }

        private void RetractBySubject(INode subject, INode predicate, INode obj, Uri graphUri)
        {
            foreach (var t in 
                GetTriplesForSubject(subject)
                    .Where(
                        t =>
                            (predicate == null || predicate is IVariableNode || t.Predicate.Equals(predicate)) &&
                            (obj == null || obj is IVariableNode || t.Object.Equals(obj)) &&
                            (graphUri == null || t.GraphUri.Equals(graphUri))))
            {
                Retract(t);
            }
        }

        private void RetractByObject(INode subject, INode predicate, INode obj, Uri graphUri)
        {
            foreach (var t in 
                GetTriplesForObject(obj)
                    .Where(t => (subject == null || subject is IVariableNode || t.Subject.Equals(subject)) &&
                                (predicate == null || predicate is IVariableNode || t.Predicate.Equals(predicate)) &&
                                (graphUri == null || t.GraphUri.Equals(graphUri))))
            {
                Retract(t);
            }
        }

        private void Retract(Triple t)
        {
            var lineStr = FormatTriple(t.Subject, t.Predicate, t.Object, t.GraphUri);
            if (!Retract(lineStr, GetFilePath(GetSubjectFileName(t.Subject)))) return;
            Retract(lineStr, GetFilePath(GetObjectFileName(t.Object)));
            Retract(lineStr, GetFilePath(GetPredicateFileName(t.Predicate)));
        }

        private string GetSubjectFileName(INode node)
        {
            var nodeStr = _lineFormat.Format(node, TripleSegment.Subject);
            return "_s" + GetPath(nodeStr);
        }

        private string GetSubjectFileName(Uri uri)
        {
            return "_s" + GetPath("<" + uri + ">");
        }

        private string GetObjectFileName(INode node)
        {
            var nodeStr = _lineFormat.Format(node, TripleSegment.Object);
            return "_o" + GetPath(nodeStr);
        }

        private string GetObjectFileName(Uri uri)
        {
            return "_o" + GetPath("<" + uri + ">");
        }

        private string GetPredicateFileName(INode node)
        {
            var nodeStr = _lineFormat.Format(node, TripleSegment.Predicate);
            return "_p" + GetPath(nodeStr);
        }

        private string GetPath(string nodeStr)
        {
            return _hasher.ComputeHash(Encoding.UTF8.GetBytes(nodeStr)).ToHexString();
        }

        private string FormatTriple(INode subject, INode predicate, INode obj, Uri graph)
        {
            var line = new StringBuilder();
            line.Append(_lineFormat.Format(subject));
            line.Append(' ');
            line.Append(_lineFormat.Format(predicate));
            line.Append(' ');
            line.Append(_lineFormat.Format(obj));
            line.Append(" <");
            line.Append(_lineFormat.FormatUri(graph));
            line.Append(">.");
            return line.ToString();
        }

        private bool Assert(TripleSegment segment, string line, string storePath, bool flushing = false)
        {
            Log.LogTrace("Assert: path={0}, data={1}", storePath, line);
            var lines = GetFileContent(storePath, flushing);
            var ix = lines.BinarySearch(line);
            if (ix >= 0) return false;
            lines.Insert(~ix, line);
            return true;
        }

        private bool Retract(string line, string storePath, bool flushing = false)
        {
            Log.LogTrace("Retract: path={0}, data={1}", storePath, line);
            var lines = GetFileContent(storePath, flushing);
            var ix = lines.BinarySearch(line);
            if (ix < 0) return false;
            lines.RemoveAt(ix);
            return true;
        }

        public void Flush()
        {
            Log.LogDebug("Flush started");
            FlushCache(_cache);
            WriteDirectoryMap();
            Log.LogDebug("Flush completed");
        }

        /// <summary>
        /// Write out a dirmap.txt file in the root store directory
        /// </summary>
        private void WriteDirectoryMap()
        {
            using (var directoryMapStream = File.Open(Path.Combine(_baseDirectory.FullName, DirMapFileName), FileMode.Create, FileAccess.Write))
            {
                using (var dirMapWriter = new StreamWriter(directoryMapStream, Encoding.UTF8))
                {
                    foreach (var entry in _directoryMap.Values)
                    {
                        dirMapWriter.WriteLine(entry);
                    }
                    dirMapWriter.Flush();
                }
            }
        }

        public IEnumerable<Triple> GetTriplesForSubject(INode subjectNode)
        {
            var subjectPath = GetSubjectFileName(subjectNode);
            var filePath = GetFilePath(subjectPath);
            var lines = GetFileContent(filePath);
            var triplesHandler = new FilteredTriplesList();
            var nodeStr = _lineFormat.Format(subjectNode, TripleSegment.Subject);
            _lineReader.Load(triplesHandler, new StringReader(string.Join("\n", lines.Where(l => l.StartsWith(nodeStr)))));
            return triplesHandler.Triples;
        }

        public IEnumerable<Triple> GetTriplesForObject(INode objectNode)
        {
            var objectPath = GetObjectFileName(objectNode);
            var filePath = GetFilePath(objectPath);
            var lines = GetFileContent(filePath);
            var triplesHandler = new FilteredTriplesList(t=>t.Object.Equals(objectNode));
            var nodeStr = _lineFormat.Format(objectNode, TripleSegment.Object);
            _lineReader.Load(triplesHandler, new StringReader(string.Join("\n", lines.Where(l=>l.Contains(nodeStr)))));
            return triplesHandler.Triples;
        }

        public void EnumerateSubjects(ITripleCollectionHandler tripleCollectionHandler)
        {
            var subjectsFilePath = Path.Combine(_baseDirectory.FullName, "_s.nq");
            if (File.Exists(subjectsFilePath))
            {
                EnumerateSubjectsInFile(new FileInfo(subjectsFilePath), tripleCollectionHandler);
            }
            var subjectsDirectoryPath = Path.Combine(_baseDirectory.FullName, "_s");
            var subjectsDirectory = new DirectoryInfo(subjectsDirectoryPath);
            if (subjectsDirectory.Exists)
            {
                IterateFiles(subjectsDirectory, f =>
                {
                    EnumerateSubjectsInFile(f, tripleCollectionHandler);
                });
            }
        }

        public void EnumerateSubjects(IResourceStatementHandler resourceStatementHandler)
        {
            var subjectsFilePath = Path.Combine(_baseDirectory.FullName, "_s.nq");
            if (File.Exists(subjectsFilePath))
            {
                EnumerateSubjectsInFile(new FileInfo(subjectsFilePath), resourceStatementHandler);
            }
            var subjectsDirectoryPath = Path.Combine(_baseDirectory.FullName, "_s");
            var subjectsDirectory = new DirectoryInfo(subjectsDirectoryPath);
            if (subjectsDirectory.Exists)
            {
                IterateFiles(subjectsDirectory, f =>
                {
                    EnumerateSubjectsInFile(f, resourceStatementHandler);
                });
            }
        }

        public void EnumerateObjects(ITripleCollectionHandler handler)
        {
            var objectsFilePath = Path.Combine(_baseDirectory.FullName, "_o.nq");
            if (File.Exists(objectsFilePath))
            {
                EnumerateObjectsInFile(new FileInfo(objectsFilePath), handler);
            }

            var objectsDirectoryPath = Path.Combine(_baseDirectory.FullName, "_o");
            var objectsDirectory = new DirectoryInfo(objectsDirectoryPath);
            if (objectsDirectory.Exists)
            {
                IterateFiles(objectsDirectory, f=> EnumerateObjectsInFile(f, handler));
            }
        }

        public void EnumerateObjects(IResourceStatementHandler handler)
        {
            var objectsFilePath = Path.Combine(_baseDirectory.FullName, "_o.nq");
            if (File.Exists(objectsFilePath))
            {
                EnumerateObjectsInFile(new FileInfo(objectsFilePath), handler);
            }

            var objectsDirectoryPath = Path.Combine(_baseDirectory.FullName, "_o");
            var objectsDirectory = new DirectoryInfo(objectsDirectoryPath);
            if (objectsDirectory.Exists)
            {
                IterateFiles(objectsDirectory, f => EnumerateObjectsInFile(f, handler));
            }
        }

        private void EnumerateSubjectsInFile(FileInfo triplesFileInfo, ITripleCollectionHandler tripleCollectionHandler)
        {
            var handler = new SubjectEnumerationHandler(tripleCollectionHandler);
            ParseNQuads(triplesFileInfo, handler);
        }

        private void EnumerateObjectsInFile(FileInfo triplesFileInfo, ITripleCollectionHandler tripleCollectionHandler)
        {
            var handler = new ObjectEnumerationHandler(tripleCollectionHandler);
            ParseNQuads(triplesFileInfo, handler);
        }

        private static void ParseNQuads(FileInfo triplesFileInfo, INodeEnumerationHandler handler)
        {
            var parser = new NQuadsParser();
            var defaultGraph = new Graph();
            using (var reader = File.OpenText(triplesFileInfo.FullName))
            {
                parser.Parse(reader, handler.HandleTriple, defaultGraph);
                handler.Flush();
            }
        }

        private void EnumerateObjectsInFile(FileInfo triplesFileInfo,
            IResourceStatementHandler resourceStatementHandler)
        {
            var handler = new ObjectEnumerationHandler(new ObjectResourceEnumerationHandler(this, resourceStatementHandler));
            ParseNQuads(triplesFileInfo, handler);
        }

        private void EnumerateSubjectsInFile(FileInfo triplesFileInfo, IResourceStatementHandler resourceStatementHandler)
        {
            var handler = new SubjectEnumerationHandler(new ResourceEnumerationHandler(this, resourceStatementHandler));
            ParseNQuads(triplesFileInfo, handler);
        }

        private class ResourceEnumerationHandler : ITripleCollectionHandler
        {
            private readonly IQuinceStore _store;
            private readonly IResourceStatementHandler _resourceStatementHandler;

            public ResourceEnumerationHandler(IQuinceStore store, IResourceStatementHandler resourceStatementHandler)
            {
                _store = store;
                _resourceStatementHandler = resourceStatementHandler;
            }

            public bool HandleTripleCollection(IList<Triple> tripleCollection)
            {
                var subject = tripleCollection[0].Subject;
                var objectStatements = _store.GetTriplesForObject(subject).ToList();
                return _resourceStatementHandler.HandleResource(subject, tripleCollection, objectStatements);
            }
        }

        private class ObjectResourceEnumerationHandler : ITripleCollectionHandler
        {
            private readonly IQuinceStore _store;
            private readonly IResourceStatementHandler _resourceStatementHandler;

            public ObjectResourceEnumerationHandler(IQuinceStore store, IResourceStatementHandler resourceStatementHandler)
            {
                _store = store;
                _resourceStatementHandler = resourceStatementHandler;
            }

            public bool HandleTripleCollection(IList<Triple> tripleCollection)
            {
                var objectNode = tripleCollection[0].Object;
                var subjectStatements = _store.GetTriplesForSubject(objectNode).ToList();
                return _resourceStatementHandler.HandleResource(objectNode, subjectStatements, tripleCollection);
            }
        }

        private interface INodeEnumerationHandler
        {
            void HandleTriple(Triple t);
            void Flush();
        }

        private class NodeEnumerationHandler : INodeEnumerationHandler
        {
            private readonly ITripleCollectionHandler _collectionHandler;
            private readonly Func<Triple, INode> _groupByFunc;
            private readonly Dictionary<INode, List<Triple>> _tripleCollections;

            protected NodeEnumerationHandler(ITripleCollectionHandler tripleCollectionHandler, Func<Triple, INode> groupBy)
            {
                _collectionHandler = tripleCollectionHandler;
                _groupByFunc = groupBy;
                _tripleCollections = new Dictionary<INode, List<Triple>>();
            }

            public void HandleTriple(Triple t)
            {
                var key = _groupByFunc(t);
                if (_tripleCollections.TryGetValue(key, out var collection))
                {
                    collection.Add(t);
                }
                else
                {
                    _tripleCollections[key] = new List<Triple>{t};
                }
            }

            public void Flush()
            {
                foreach (var k in _tripleCollections.Keys)
                {
                    _collectionHandler.HandleTripleCollection(_tripleCollections[k]);
                }
                _tripleCollections.Clear();
            }
        }

        private class SubjectEnumerationHandler : INodeEnumerationHandler
        {
            private readonly ITripleCollectionHandler _tripleCollectionHandler;
            private INode _currentKey;
            private List<Triple> _currentCollection;

            public SubjectEnumerationHandler(ITripleCollectionHandler tripleCollectionHandler)
            {
                _tripleCollectionHandler = tripleCollectionHandler;
            }

            public void HandleTriple(Triple t)
            {
                var key = t.Subject;
                if (_currentKey == null)
                {
                    _currentKey = key;
                    _currentCollection = new List<Triple> { t };
                }
                else
                {
                    if (_currentKey.Equals(key))
                    {
                        _currentCollection.Add(t);
                    }
                    else
                    {
                        if (_currentCollection.Count > 0)
                        {
                            _tripleCollectionHandler.HandleTripleCollection(_currentCollection);
                        }
                        _currentKey = key;
                        _currentCollection = new List<Triple> { t };
                    }
                }
            }

            public void Flush()
            {
                if (_currentCollection != null && _currentCollection.Count > 0)
                {
                    _tripleCollectionHandler.HandleTripleCollection(_currentCollection);
                }
                _currentKey = null;
                _currentCollection = null;
            }
        }

        private class ObjectEnumerationHandler : NodeEnumerationHandler
        {
            public ObjectEnumerationHandler(ITripleCollectionHandler tripleCollectionHandler) : base(
                tripleCollectionHandler, t => t.Object)
            {
            }
        }

        public IEnumerable<Triple> GetTriplesForSubject(Uri shapeUri)
        {
            var subject = "<" + shapeUri + ">";
            var subjectPath = GetSubjectFileName(shapeUri);
            var filePath = GetFilePath(subjectPath);
            var lines = GetFileContent(filePath);
            var triplesHandler = new FilteredTriplesList();
            _lineReader.Load(triplesHandler, new StringReader(string.Join("\n", lines.Where(l=>l.StartsWith(subject)))));
            return triplesHandler.Triples;
        }

        public IEnumerable<Triple> GetTriplesForObject(Uri objectUri)
        {
            var objectPath = GetObjectFileName(objectUri);
            var filePath = GetFilePath(objectPath);
            var lines = GetFileContent(filePath);
            var triplesHandler = new FilteredTriplesList(t => (t.Object as IUriNode)?.Uri.Equals(objectUri) ?? false);
            var nodeStr = "<" + objectUri + ">";
            _lineReader.Load(triplesHandler, new StringReader(string.Join("\n", lines.Where(l => l.Contains(nodeStr)))));
            return triplesHandler.Triples;
        }

        private List<string> GetFileContent(string path, bool noFlush = false)
        {
            if (_cache.ContainsKey(path))
            {
                return _cache[path];
            }

            List<string> ret;

            var fullPath = Path.Combine(_baseDirectory.FullName, path);
            if (File.Exists(fullPath))
            {
                ret = new List<string>(File.ReadAllLines(fullPath, Encoding.UTF8));
                _cache[path] = ret;
            }
            else
            {
                ret = new List<string>();
                _cache[path] = ret;
            }

            if (!noFlush && _cache.Count >= _cacheThreshold)
            {
                FlushCache(_cache);
            }
            return ret;
        }

        private Trie<string, string, string> BuildDirectoryMap(DirectoryInfo rootDir)
        {
            var trie = new Trie<string, string, string>(KeyMapper);
            foreach (var childDir in rootDir.EnumerateDirectories())
            {
                if (childDir.Name == ".git") continue;
                trie.Add(childDir.Name, childDir.Name);
                IndexDirectories(childDir.Name, childDir.Name, trie);
            }
            return trie;
        }

        private void IndexDirectories(string prefix, string parentPath,
            Trie<string, string, string> trie)
        {
            DirectoryInfo parent = new DirectoryInfo(Path.Combine(_baseDirectory.FullName, parentPath));
            foreach (var childDir in parent.EnumerateDirectories())
            {
                if (childDir.Name == ".git") continue;
                
                var childPath = Path.Combine(parentPath, childDir.Name);
                trie.Add(prefix+childDir.Name, childPath);
                IndexDirectories(prefix+childDir.Name, childPath, trie);
            }
        }

        private static IEnumerable<string> KeyMapper(string key)
        {
            for (var i = 0; i < key.Length; i+= 2)
            {
                yield return key.Substring(i, 2);
            }
        }

        private string GetFilePath(string fileName)
        {
            var trieNode = _directoryMap.FindPredecessor(fileName);
            if (trieNode == null) return fileName.Substring(0, 2) + FileNameExtension;
            var nodeDepth = GetNodeDepth(trieNode);
            return (nodeDepth * 2 == fileName.Length
                       ? fileName
                       : Path.Combine(trieNode.Value, fileName.Substring(nodeDepth * 2, 2))) 
                       + FileNameExtension;
        }

        private static int GetNodeDepth<TKeyBit, TValue>(ITrieNode<TKeyBit, TValue> n) where TValue : class
        {
            var depth = 0;
            while (!n.IsRoot)
            {
                depth++;
                n = n.Parent;
            }
            return depth;
        }

        private void FlushCache(Dictionary<string, List<string>> cache)
        {
            var cacheCount = cache.Count;
            var timer = new Stopwatch();
            timer.Start();

            var toFlush = cache.Keys.ToList();
            foreach (var fileName in toFlush)
            {
                var lines = cache[fileName];
                if (lines.Count > _splitThreshold)
                {
                    var segment = fileName.StartsWith("_s") ? TripleSegment.Subject :
                        fileName.StartsWith("_p") ? TripleSegment.Predicate : TripleSegment.Object;
                    if (CanSplit(lines, segment))
                    {
                        SplitFile(fileName, lines, segment);
                    }
                }
            }

            foreach (var pair in cache)
            {
                var targetPath = Path.Combine(_baseDirectory.FullName, pair.Key);
                var targetDir = Path.GetDirectoryName(targetPath);
                if (targetDir != null && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
            }

            Parallel.ForEach(cache, (pair) =>
            {
                var targetPath = Path.Combine(_baseDirectory.FullName, pair.Key);
                File.WriteAllLines(targetPath, pair.Value, Encoding.UTF8);
            });

            cache.Clear();
            timer.Stop();
            Log.LogDebug("FlushCache: {0} entries written in {1} seconds", cacheCount, timer.Elapsed.TotalSeconds);
        }


        private bool CanSplit(IReadOnlyList<string> lines, TripleSegment segment)
        {
            return lines.Count >= _splitThreshold && CanSplitParser(lines, segment);
            //return CanSplitRegex(lines, segment);
        }

        // KA - Regex-based checking is slightly faster than using the DNR parser, but the parser is less likely to be wrong :-)
        /*
        private Regex SplitRegex = new Regex("(?<s>(_:[^\\s]+)|(<[^>]+>))\\s+(?<p>(_:[^\\s]+)|(<[^>]+>))\\s+(?<o>(_:[^\\s]+)|(<[^>]+>)|\"[^\"]*\"(@\\S+)?(\\^\\^<[^>]+>)?)\\s+(?<g>(_:[^\\s]+)|(<[^>]+>))\\s*\\.");

        private bool CanSplitRegex(IReadOnlyList<string> lines, TripleSegment segment)
        {
            var segmentGroup = segment == TripleSegment.Subject ? "s" : segment == TripleSegment.Predicate ? "p" : "o";
            var firstNode = SplitRegex.Match(lines[0]).Groups[segmentGroup].Value;
            return lines.Skip(1).Any(l => SplitRegex.Match(l).Groups[segmentGroup].Value != firstNode);
        }
        */

        private bool CanSplitParser(IReadOnlyList<string> lines, TripleSegment segment)
        {
            var singleLineHandler = new SingleLineHandler();
            _lineReader.Load(singleLineHandler, new StringReader(lines[0]));
            var firstNode = segment == TripleSegment.Subject
                ? singleLineHandler.LastTriple.Subject
                : segment == TripleSegment.Predicate
                    ? singleLineHandler.LastTriple.Predicate
                    : singleLineHandler.LastTriple.Object;
            var matchHandler = new WhileMatchHandler(segment, firstNode);
            _lineReader.Load(matchHandler, new StringReader(string.Join("\n", lines)));
            return !matchHandler.Match;
        }

        private string _splitDirPath;

        private void SplitFile(string filePath, IEnumerable<string> lines, TripleSegment tripleSegment)
        {
            Log.LogDebug("SplitFile: {0}", filePath);
            _splitDirPath = filePath.Substring(0, filePath.IndexOf('.'));
            if (!Directory.Exists(Path.Combine(_baseDirectory.FullName, _splitDirPath)))
            {
                Directory.CreateDirectory(Path.Combine(_baseDirectory.FullName, _splitDirPath));
                var prefixString =
                    _splitDirPath.Replace(Path.DirectorySeparatorChar.ToString(), "")
                        .Replace(Path.AltDirectorySeparatorChar.ToString(), "");
                _directoryMap.Add(prefixString, _splitDirPath);
            }
            
            var handler = new ImportHandler(this, tripleSegment);
            _lineReader.Load(handler, new StringReader(string.Join("\n", lines)));

            // Drop the old file
            _cache.Remove(filePath);
            var deletePath = Path.Combine(_baseDirectory.FullName, filePath);
            if (File.Exists(deletePath)) File.Delete(deletePath);
        }

        private bool HandleTriple(Triple t, TripleSegment tripleSegment)
        {
            var line = FormatTriple(t.Subject, t.Predicate, t.Object, t.GraphUri);
            var targetFileName = GetTripleSegmentFileName(t, tripleSegment);
            var targetPath = GetFilePath(targetFileName);
            Assert(tripleSegment, line, targetPath, true);
            return true;
        }

        private string GetTripleSegmentFileName(Triple t, TripleSegment segment)
        {
            switch (segment)
            {
                    case TripleSegment.Subject:
                        return GetSubjectFileName(t.Subject);
                    case TripleSegment.Predicate:
                        return GetPredicateFileName(t.Predicate);
                    case TripleSegment.Object:
                        return GetObjectFileName(t.Object);
                default:
                    throw new ArgumentException("Segment must be subject, predicate or object", nameof(segment));
            }
        }
       
        private class ImportHandler : BaseRdfHandler
        {
            private readonly DynamicFileStore _fileStore;
            private readonly TripleSegment _importSegment;

            public ImportHandler(DynamicFileStore fileStore, TripleSegment importSegment)
            {
                _fileStore = fileStore;
                _importSegment = importSegment;
            }

            protected override bool HandleTripleInternal(Triple t)
            {
                return _fileStore.HandleTriple(t, _importSegment);
            }

            public override bool AcceptsAll => true;

        }
    }
}
