using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    public class MetricAssetViewModel
    {
        #region From metric asset itself

        public Guid Uid { get; set; }

        public Guid? ParentUid { get; set; }

        public Guid AssetTypeUid { get; set; }

        public bool IsGroup { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        #endregion

        #region From metric asset version

        public DateTime EffectiveDate { get; set; }

        public decimal Weight { get; set; }

        [StringLength(1)]
        public string ConditionAndOr { get; set; }

        #endregion

        #region From metric asset version condition

        public List<MetricAssetVersionConditionViewModel> Conditions { get; set; } = new List<MetricAssetVersionConditionViewModel>();

        #endregion
    }

    public class MetricAssetVersionConditionViewModel
    {
        public int FieldTypeID { get; set; }

        [StringLength(10)]
        public string Operator { get; set; }

        public string Values { get; set; }
    }

    public class MetricFieldTypeViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<MetricFieldTypeValueViewModel> Values { get; set; }
    }

    public class MetricFieldTypeValueViewModel
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
}
