using System;
using System.Runtime.Serialization;

namespace d360.web.Services
{
    [Serializable]
    public class RecoverableBusinessLayerException : BusinessLayerException
    {
        public RecoverableBusinessLayerException()
        {
        }

        public RecoverableBusinessLayerException(string message) : base(message)
        {
        }

        public RecoverableBusinessLayerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RecoverableBusinessLayerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
