using System;

namespace d360.web.Services
{
    internal sealed class ResourceIsExistsRequest : IsEntityExistsRequest
    {
        public Guid? Uid { get; set; }
    }
}