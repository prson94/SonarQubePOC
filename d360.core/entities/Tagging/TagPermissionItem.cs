using System;

namespace d360.core.entities
{
    public class TagPermissionItem
    {
        public Guid uid { get; set; }

        public string Value { get; set; }

        public bool CanDelete { get; set; }
    }
}
