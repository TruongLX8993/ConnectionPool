using System;
using System.Runtime.Serialization;

namespace ConnectionPool.Exceptions
{
    public class StateException : ConnectionPoolException
    {
        public StateException(ConnectionState fromState,ConnectionState toState) :
            base($"Can not switch from {fromState.ToString()} to new state {toState.ToString()}")
        {
        }

        public StateException(string message) : base(message)
        {
        }

        public StateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected StateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}