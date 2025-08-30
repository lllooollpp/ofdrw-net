using System;

namespace OfdrwNet.Layout.Exceptions
{
    public class DocReadException : System.Exception
    {
        public DocReadException() { }
        public DocReadException(string message) : base(message) { }
        public DocReadException(string message, System.Exception inner) : base(message, inner) { }
    }
}
