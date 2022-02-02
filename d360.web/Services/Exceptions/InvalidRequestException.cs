using System;
using System.Runtime.Serialization;

namespace d360.web.Services
{
    [Serializable]
    public class InvalidRequestException : UnrecoverableBusinessLayerException
    {
        public InvalidRequestException()
        {
        }

        public InvalidRequestException(string message) : base(message)
        {
        }

        public InvalidRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
