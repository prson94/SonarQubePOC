using System.Collections.Generic;

namespace d360.core.entities
{
    public class BulkLoadGetLoadColumnsModel : List<BulkLoadGetLoadColumnModel>
    {
    }

    public class BulkLoadGetLoadColumnModel
    {
        public string Name { get; set; }

        public bool Required { get; set; }

        public bool PartOfKey { get; set; }

        public bool IsLookup { get; set; }

        public int? FieldTypeId { get; set; }

        public bool AllowMultipleValues { get; set; }

        public int Level { get; set; }

        public List<BulkLoadGetLoadColumnModelValue> Lookups { get; set; }
    }

    public class BulkLoadGetLoadColumnModelValue
    {
        public string Value { get; set; }

        public string Label { get; set; }
    }
}
