using System;
using System.Collections.Generic;
using VDS.RDF;

namespace NetworkedPlanet.Quince
{
    public interface IQuinceStore
    {
        /// <summary>
        /// Adds a quad to the store if it does not already exist
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <param name="predicate">The predicate</param>
        /// <param name="obj">The object</param>
        /// <param name="graph">The graph</param>
        /// <remarks>All 4 parameters are required and must not be null.</remarks>
        void Assert(INode subject, INode predicate, INode obj, Uri graph);

        /// <summary>
        /// Retracts a quad from the store if it exists
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <param name="predicate">The predicate</param>
        /// <param name="obj">The object</param>
        /// <param name="graph">The graph</param>
        /// <remarks></remarks>
        void Retract(INode subject, INode predicate, INode obj, Uri graph);

        /// <summary>
        /// Add all the quads in the specified graph to the store
        /// </summary>
        /// <param name="graph">The graph to be added to the store</param>
        void Assert(IGraph graph);

        /// <summary>
        /// Retracts all of the triples in the specified graph
        /// </summary>
        /// <param name="graph">The graph to be dropped</param>
        void DropGraph(Uri graph);

        /// <summary>
        /// Commit all pending changes to disk
        /// </summary>
        void Flush();

        /// <summary>
        /// Retrieves all quads for a given subject
        /// </summary>
        /// <param name="subjectNode">The subject to retrieve quads for</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesForSubject(INode subjectNode);

        /// <summary>
        /// Retrieves all quads for a given subject
        /// </summary>
        /// <param name="subjectUri">The subject to retrieve quads for</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesForSubject(Uri subjectUri);

        /// <summary>
        /// Retrieves all quads for a given object
        /// </summary>
        /// <param name="objectNode">The object to retrieve quads for</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesForObject(INode objectNode);

        /// <summary>
        /// Retrieves all quads for a given object
        /// </summary>
        /// <param name="objectUri">The object to retrieve quads for</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesForObject(Uri objectUri);

        /// <summary>
        /// Retrieves all triples in the repository, grouping triples
        /// by common subject
        /// </summary>
        /// <param name="handler">The handler to be invoked with each <see cref="TripleCollection"/></param>
        /// <returns>An enumeration of <see cref="TripleCollection"/> instances where
        /// each <see cref="TripleCollection"/> contains all the triples in the repository
        /// with the same subject</returns>
        void EnumerateSubjects(ITripleCollectionHandler handler);

        /// <summary>
        /// Enumerates all subjects in the repository
        /// </summary>
        /// <param name="handler">A handler that will receive the resource node, the collection of statements with that resource as subject and the collection of statements with that resource as object</param>
        void EnumerateSubjects(IResourceStatementHandler handler);

        /// <summary>
        /// Enumerate all object nodes in the repository
        /// </summary>
        /// <param name="handler">A handler invoked for each unique object node, that will receive the object node itself and the collection of statements with that node as object</param>
        void EnumerateObjects(ITripleCollectionHandler handler);

        /// <summary>
        /// Enumerate all object nodes in the repository
        /// </summary>
        /// <param name="handler">A handler that will receive the object node, the collection of statements with that node as subject and the collection of statements with that node as object</param>
        void EnumerateObjects(IResourceStatementHandler handler);
    }
}
