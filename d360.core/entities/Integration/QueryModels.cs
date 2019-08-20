using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Integration
{
    public class ExecutionsSinceClearedHashes
    {
        public static string Query {
            get {
                return @"select	* from (
        select count(1) as FieldExecutionCount from integration.ExecutionAssetType where SynchedAssetTypeID = @SynchedAssetTypeID and IsFullRefresh = 1 and ExecutionID >= (
            select coalesce(max(ExecutionID), 0) from integration.ExecutionAssetType where SynchedAssetTypeID = @SynchedAssetTypeID and FieldHashesCleared = 1
        ) ) F
        full join (
        select count(1) as RelationExecutionCount from integration.ExecutionAssetType where SynchedAssetTypeID = @SynchedAssetTypeID and IsFullRefresh = 1 and ExecutionID >= (
            select coalesce(max(ExecutionID), 0) from integration.ExecutionAssetType where SynchedAssetTypeID = @SynchedAssetTypeID and RelationshipHashesCleared = 1
        ) ) R on  1 = 1
        full join (
        select count(1) as OwnerExecutionCount from integration.ExecutionAssetType where SynchedAssetTypeID = @SynchedAssetTypeID and IsFullRefresh = 1 and ExecutionID >= (
            select coalesce(max(ExecutionID), 0) from integration.ExecutionAssetType where SynchedAssetTypeID = @SynchedAssetTypeID and OwnershipHashesCleared = 1
        ) ) O on  1 = 1";
            }
        }

        public int FieldExecutionCount { get; set; }
        public int RelationExecutionCount { get; set; }
        public int OwnerExecutionCount { get; set; }

    }
}
