using System;

namespace NetworkedPlanet.Quince
{
    public class RdfParserException : Exception
    {
        public int LineNumber { get; }

        public RdfParserException(string message, Exception innerException = null):base(message, innerException) { }
        public RdfParserException(int lineNumber, string message, Exception innerException = null) : base(message, innerException)
        {
            LineNumber = lineNumber;
        }
    }
}
