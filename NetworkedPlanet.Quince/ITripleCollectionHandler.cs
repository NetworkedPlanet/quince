using System.Collections.Generic;
using VDS.RDF;

namespace NetworkedPlanet.Quince
{
    public interface ITripleCollectionHandler
    {
        /// <summary>
        /// Handle a single triple collection from a generator
        /// </summary>
        /// <param name="tripleCollection"></param>
        /// <returns>True to continue receiving more triple collections, false to stop the generator</returns>
        bool HandleTripleCollection(IList<Triple> tripleCollection);
    }
}