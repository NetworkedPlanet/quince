using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Writing.Formatting;

namespace NetworkedPlanet.Quince.Repository
{
    /// <summary>
    /// Representation of a Quince Repository diff as a collection of triples added and triples deleted
    /// </summary>
    public class QuinceDiff
    {
        private readonly List<Triple> _inserted;
        private readonly List<Triple> _deleted;

        public IEnumerable<Triple> DeletedTriples => _deleted;
        public IEnumerable<Triple> InsertedTriples => _inserted;

        public QuinceDiff()
        {
            _inserted = new List<Triple>();
            _deleted = new List<Triple>();
        }

        public void Deleted(Triple t)
        {
            _deleted.Add(t);
        }

        public void Inserted(Triple t)
        {
            _inserted.Add(t);
        }

        // TODO: Add support for "compressing" the diff by eliminating those triples that occur in both collections


        /// <summary>
        /// Create a set of SPARQL update commands that will apply the insertion and deletions recorded by this diff
        /// to a SPARQL store
        /// </summary>
        /// <returns></returns>
        public string AsSparqlUpdate()
        {
            var update = new StringBuilder();
            update.AppendLine("DELETE DATA {");
            update.Append(GetGraphTriples(_deleted));
            update.AppendLine("}");
            update.AppendLine("INSERT DATA {");
            update.Append(GetGraphTriples(_inserted));
            update.Append("}");
            return update.ToString();
        }

        private static string GetGraphTriples(IEnumerable<Triple> triples)
        {
            var lineFormat = new NQuads11Formatter();
            var str = new StringBuilder();
            foreach (var graphGroup in triples.GroupBy(t => t.GraphUri))
            {
                str.Append("GRAPH <");
                str.Append(graphGroup.Key);
                str.AppendLine("> {");
                foreach (var t in graphGroup)
                {
                    str.Append("  ");
                    str.Append(lineFormat.Format(t.Subject));
                    str.Append(" ");
                    str.Append(lineFormat.Format(t.Predicate));
                    str.Append(" ");
                    str.Append(lineFormat.Format(t.Object));
                    str.AppendLine(" .");
                }
                str.AppendLine("}");
            }
            return str.ToString();
        }
    }
}
