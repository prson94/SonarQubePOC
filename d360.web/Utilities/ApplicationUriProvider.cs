using System;
using System.Net.Http;

namespace d360.web.Utilities
{
    internal sealed class ApplicationUriProvider : IApplicationUriProvider
    {
        public string GetExecutionByIdLink(HttpRequestMessage request, Guid id)
        {
            return $"{request.RequestUri.Scheme}://{request.RequestUri.Host}/api/v2/executions/{id}";
        }
    }
}
