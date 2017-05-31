using System.Collections.Generic;
using VDS.RDF;

namespace NetworkedPlanet.Quince
{
    public interface IResourceStatementHandler
    {
        bool HandleResource(INode resourceNode, IList<Triple> subjectStatements, IList<Triple> objectStatements);
    }
}