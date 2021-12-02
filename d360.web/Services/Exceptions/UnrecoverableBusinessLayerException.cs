using System;
using System.Runtime.Serialization;

namespace d360.web.Services
{
    [Serializable]
    public class UnrecoverableBusinessLayerException : BusinessLayerException
    {
        public UnrecoverableBusinessLayerException()
        {
        }

        public UnrecoverableBusinessLayerException(string message) : base(message)
        {
        }

        public UnrecoverableBusinessLayerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnrecoverableBusinessLayerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}