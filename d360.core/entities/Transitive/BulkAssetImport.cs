using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Name="assets")]
    public class BulkAssetImport : List<Dictionary<string, string>>
    {

    }

    public class AssetImportResult
    {
        public int ItemNumber { get; set; }
        public string SourceID { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}
