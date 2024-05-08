using System;
using System.Runtime.Serialization;

namespace d360.web.Services
{
    [Serializable]
    public class InvalidRequestException : UnrecoverableBusinessLayerException
    {
        public InvalidRequestException(string message) : base(message)
        {
        }
    }
}
