using System;
using System.Runtime.Serialization;

namespace ConnectionPool.Exceptions
{
    public class ConnectionPoolException : Exception
    {
        public ConnectionPoolException()
        {
        }

        public ConnectionPoolException(string message) : base(message)
        {
        }

        public ConnectionPoolException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConnectionPoolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}