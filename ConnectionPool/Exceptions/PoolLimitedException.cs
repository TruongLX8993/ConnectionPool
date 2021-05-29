using System;
using System.Runtime.Serialization;

namespace ConnectionPool.Exceptions
{
    public class PoolLimitedException : ConnectionPoolException
    {
        public PoolLimitedException()
        {
        }

        public PoolLimitedException(string message) : base(message)
        {
        }

        public PoolLimitedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PoolLimitedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}