using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing;

namespace NetworkedPlanet.Quince
{
    internal class SingleLineHandler : BaseRdfHandler
    {
        public Triple LastTriple { get; private set; }

        protected override bool HandleTripleInternal(Triple t)
        {
            LastTriple = t;
            return true;
        }

        public override bool AcceptsAll => true;
    }

    internal class WhileMatchHandler : BaseRdfHandler
    {
        public bool Match { get; private set; }
        private readonly TripleSegment _segmentToMatch;
        private readonly INode _nodeToMatch;


        public WhileMatchHandler(TripleSegment segment, INode node)
        {
            _segmentToMatch = segment;
            _nodeToMatch = node;
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            switch (_segmentToMatch)
            {
                    case TripleSegment.Subject:
                        Match = t.Subject.Equals(_nodeToMatch);
                    break;
                    case TripleSegment.Predicate:
                        Match = t.Predicate.Equals(_nodeToMatch);
                    break;
                    case TripleSegment.Object:
                        Match = t.Object.Equals(_nodeToMatch);
                    break;
            }
            return Match;
        }

        public override bool AcceptsAll => true;
    }
}