using System;
using System.Runtime.Serialization;

using Resources;

namespace d360.web.Services
{
    [Serializable]
    public class UnauthorizedBusinessLayerException : UnrecoverableBusinessLayerException
    {
        public UnauthorizedBusinessLayerException() : this(ApiMessages.EndpointNotAuthorizedMessage)
        {
        }

        public UnauthorizedBusinessLayerException(string message) : base(message)
        {
        }
    }
}
