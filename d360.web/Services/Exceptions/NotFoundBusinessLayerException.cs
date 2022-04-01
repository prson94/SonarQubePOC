using System;
using System.Runtime.Serialization;

namespace d360.web.Services
{
    [Serializable]
    public class NotFoundBusinessLayerException : UnrecoverableBusinessLayerException
    {
        public NotFoundBusinessLayerException()
        {
        }

        public NotFoundBusinessLayerException(string message) : base(message)
        {
        }

        public NotFoundBusinessLayerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotFoundBusinessLayerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
