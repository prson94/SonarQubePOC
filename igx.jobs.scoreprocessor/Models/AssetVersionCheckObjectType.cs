using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AssetVersionCheckObjectTypes : List<AssetVersionCheckObjectType>
    {
        public bool OkToAddToList(Guid versionUid)
        {
            return !this.Any(i => i.AssetVersionUid == versionUid);
        }

        public bool ShouldContinueAnalysis(Guid versionUid)
        {
            var shouldContinue = true;
            var item = this.FirstOrDefault(i => i.AssetVersionUid == versionUid);
            if (item != null)
            {
                shouldContinue = item.Valid;
            }
            else
            {
                shouldContinue = true;
            }
            return shouldContinue;
        }
    }

    internal class AssetVersionCheckObjectType
    {
        public Guid AssetVersionUid { get; set; }
        public Guid? TypeUid { get; set; }
        public bool Valid { get; set; }
    }
}
