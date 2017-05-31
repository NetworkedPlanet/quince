using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace NetworkedPlanet.Quince
{
    public class FilteredTriplesList : BaseRdfHandler
    {
        private readonly Func<Triple, bool> _filter;
        private readonly List<Triple> _triples;

        public FilteredTriplesList(Func<Triple, bool> filterFunc = null )
        {
            _filter = filterFunc;
            _triples = new List<Triple>();
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            if (_filter == null || _filter(t))
            {
                _triples.Add(t);
            }
            return true;
        }

        public override bool AcceptsAll => true;

        public IEnumerable<Triple> Triples => _triples;
    }
}