using System;
using System.Net.Http;

namespace d360.web.Utilities
{
    public interface IApplicationUriProvider
    {
        [Obsolete("This method should work without using of request parameter")]
        string GetExecutionByIdLink(HttpRequestMessage request, Guid id);
    }
}