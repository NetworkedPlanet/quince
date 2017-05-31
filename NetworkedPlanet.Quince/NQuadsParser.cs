using System;
using System.Collections.Generic;
using System.IO;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;

namespace NetworkedPlanet.Quince
{

    public class NQuadsParser : BaseRdfHandler
    {
        private Action<Triple> _sinkAction;

        public void Parse(IEnumerable<string> lines, Action<Triple> sink, IGraph defaultGraph)
        {
            _sinkAction = sink;
            var parser = new VDS.RDF.Parsing.NQuadsParser(NQuadsSyntax.Rdf11);
            var allLines = string.Join("\n", lines);
            using (var reader = new StringReader(allLines))
            {
                parser.Load(this, reader);
            }
        }

        public void Parse(TextReader reader, Action<Triple> sink, IGraph defaultGraph)
        {
            _sinkAction = sink;
            var parser = new VDS.RDF.Parsing.NQuadsParser(NQuadsSyntax.Rdf11);
            parser.Load(this, reader);
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            _sinkAction(t);
            return true;
        }

        public override bool AcceptsAll => true;
    }

}