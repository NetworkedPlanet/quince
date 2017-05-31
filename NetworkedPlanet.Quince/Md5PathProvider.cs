using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VDS.RDF;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace NetworkedPlanet.Quince
{
    internal class Md5PathProvider : IPathProvider
    {
        private readonly int _dirSize;
        private readonly int _dirDepth;
        private readonly int _fileNameSize;

        private readonly MD5 _hasher;
        private readonly INodeFormatter _formatter;
        public Md5PathProvider(int dirSize, int dirDepth, int fileNameSize = 8)
        {
            _dirSize = dirSize;
            _dirDepth = dirDepth;
            _fileNameSize = fileNameSize;
            _hasher = MD5.Create();
            _formatter = new NQuads11Formatter();
        }

        public string GetSubjectPath(INode node)
        {
            var nodeStr = _formatter.Format(node, TripleSegment.Subject);
            return GetPath(nodeStr) + ".sub";
        }

        public string GetObjectPath(INode node)
        {
            var nodeStr = _formatter.Format(node, TripleSegment.Subject);
            var p = GetPath(nodeStr) + ".obj";
            return Path.Combine("_obj",  p);
        }

        private string GetPath(string nodeStr)
        {
            var hash = _hasher.ComputeHash(Encoding.UTF8.GetBytes(nodeStr));
            var fileName = hash.ToHexString();
            var path = new StringBuilder();
            for (var i = 0; i < _dirDepth; i++)
            {
                var dirName = fileName.Skip(i * _dirSize).Take(_dirSize).ToArray();
                path.Append(dirName);
                path.Append(Path.DirectorySeparatorChar);
            }
            if (_fileNameSize < 0)
            {
                path.Append(fileName.Skip(_dirDepth*_dirSize).Take(-1*_fileNameSize).ToArray());
            }
            else
            {
                path.Append(fileName.Substring(0, _fileNameSize));
            }
            return path.ToString();
        }
    }
}