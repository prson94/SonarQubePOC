using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    public class DataQualityInsertModel
    {
        public Guid OwningAssetUid;
        public Guid? EvaluatedAssetUid;
        public DateTime EffectiveDate;
        public DateTime RunDate;
        [Range(0, 9223372036854775807,
            ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public long PassCount;
        [Range(0, 9223372036854775807,
            ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public long FailCount;
    }

}
