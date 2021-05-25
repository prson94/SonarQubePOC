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
        public bool OkToAddToList(Object locker, Guid versionUid)
        {
            var ok = false;
            lock (locker)
            {
                ok = !this.Any(i => i.AssetVersionUid == versionUid);
            }
            return ok;
        }

        public bool ShouldContinueAnalysis(Object locker, Guid versionUid)
        {
            var shouldContinue = true;

            lock (locker) {
                var item = this.FirstOrDefault(i => i.AssetVersionUid == versionUid);
                if (item != null)
                {
                    shouldContinue = item.Valid;
                }
                else
                {
                    shouldContinue = true;
                }
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
