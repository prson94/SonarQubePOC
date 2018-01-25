using d360.core.enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    public class AssetDetail
    {
        public long ID { get; set; }

        public string DisplayValue { get; set; }

        public int AssetTypeID { get; set; }

        public State State { get; set; }

        public string Object { get; set; }

        public int ObjectID { get; set; }

        public string AssetTypeName { get; set; }

        public string Type { get; set; }

        public int TypeID { get; set; }

        public DateTime?  CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

    }
}
