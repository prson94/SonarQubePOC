using System;
using System.Runtime.Serialization;
using d360.core.resources;

namespace d360.web.Services
{
    [Serializable]
    public class UnauthorizedBusinessLayerException : UnrecoverableBusinessLayerException
    {
        public UnauthorizedBusinessLayerException() : this(Error.EndpointNotAuthorizedMessage)
        {
        }

        public UnauthorizedBusinessLayerException(string message) : base(message)
        {
        }
    }
}
