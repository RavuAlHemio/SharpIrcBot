using System;
using System.Runtime.Serialization;

namespace SharpIrcBot.Plugins.Calc
{
    [Serializable]
    public class SimplificationException : System.Exception
    {
        public SimplificationException(string message) : base(message) {}
        public SimplificationException(string message, Exception inner) : base(message, inner) {}
        protected SimplificationException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
