using System;
using System.Runtime.Serialization;

namespace SharpIrcBot.Plugins.Calc
{
    [Serializable]
    public class FunctionDomainException : System.Exception
    {
        public FunctionDomainException() : base() {}
        public FunctionDomainException(string message) : base(message) {}
        public FunctionDomainException(string message, Exception inner) : base(message, inner) {}
        protected FunctionDomainException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
