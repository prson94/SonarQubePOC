using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class BulkOwnerImport
    {
        public string UserIdFieldName { get; set; }

        public IList<OwnerImportRequest> Items { get; set; }
    }

    public class OwnerImportRequest
    {
        public int ItemNumber { get; set; }
        public string SourceID { get; set; }
        public string RoleName { get; set; }
        public string UserId { get; set; }

        public string Message { get; set; }
        public bool Success { get; set; }
    }
}
