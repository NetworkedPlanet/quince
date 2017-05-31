using System;

namespace NetworkedPlanet.Quince
{
    public class QuinceException : Exception
    {
        public QuinceException(string msg) : base(msg) { }
        public QuinceException(string msg, Exception inner):base(msg, inner) { }
    }
}