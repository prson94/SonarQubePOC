using System;

namespace d360.web.Services
{
    internal sealed class ResponsibilityTypeIsExistsRequest : IsEntityExistsRequest
    {
        public Guid? Uid { get; set; }
    }
}