using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AllocationDataModelConditionItem
    {
        public Guid ItemUid { get; set; }
        public MetricConditionType ConditionType { get; set; }
        public int? ConditionFieldTypeID { get; set; }
        public int? ConditionIntersectTypeID { get; set; }
        public Operator Operator { get; set; }
        public List<AllocationDataModelConditionItemTempValue> ValueItems { get; set; }
        public List<string> Values
        {
            get
            {
                if (ValueItems != null)
                {
                    return ValueItems.Select(i => i.Value).ToList();
                }
                else
                {
                    return new List<string>();
                }
            }
        }
    }
}
