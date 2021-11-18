using System;
using System.Runtime.Serialization;

namespace d360.web.Services
{
    [Serializable]
    public abstract class BusinessLayerException : Exception
    {
        protected BusinessLayerException()
        {
        }

        protected BusinessLayerException(string message) : base(message)
        {
        }

        protected BusinessLayerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BusinessLayerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}