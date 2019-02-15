using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<Group> Groups { get; set; }

        public DbSet<ResourceGroup> ResourceGroups { get; set; }

        public DbSet<ResourcePasswordReset> ResourcePasswordResets { get; set; }

        public DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }                              /* VIEW */

        public DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

        public DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }

        public DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

        public DbSet<ResponsibilityTypeRelationRule> ResponsibilityTypeRelationRules { get; set; }

        public DbSet<GlobalReportingResource> GlobalReportingResources { get; set; }

        #endregion

        #region Engine Methods

        public IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByAsset(long id)
        {
            try
            {
                return Database.Connection.Query<ResponsibilityType>(@"
select	RT.*
from	ResponsibilityType RT
		inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeID = RT.ID
		inner join AssetType T on T.Object = R.ObjectType and T.ObjectID = R.ObjectID
		inner join Asset A on A.AssetTypeID = T.ID and A.ID = @id
order by RT.Name", new { id }).AsQueryable();
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility Type");
            }
            catch
            {
                throw;
            }
        }


        public List<PermissionInfo> GetTypePermissions(string type, int typeID)
        {
            var permissions = Permission.DeleteAsset.GetList();
            
            var responsibilityAssignments = Filter<ResponsibilityDetail>(i => 
                i.Type == type && i.TypeID == typeID && 
                i.AssetID == 0 && 
                i.ResourceID == CurrentResourceID
            ).Select(i => i.PermissionsBitMask).Distinct().ToList();

            permissions.ForEach(p =>
            {
                p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
            });

            permissions.RemoveAll(i => !i.Selected);

            return permissions;
        }

        public List<PermissionInfo> GetPermissions(long assetId, int assetTypeId)
        {
            var permissions = Permission.DeleteAsset.GetList();

            var responsibilityAssignments = Query<int>(@"select PermissionsBitMask from ResponsibilityAllAsset where ResourceID = @ResourceID and AssetTypeID = @AssetTypeID
                                                                    union select PermissionsBitMask from ResponsibilityAllAsset where ResourceID = @ResourceID and AssetID = @AssetID ", new { ResourceID = CurrentResourceID, AssetTypeID = assetTypeId, AssetID = assetId });
                   
            permissions.ForEach(p =>
            {
                p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
            });

            permissions.RemoveAll(i => !i.Selected);

            return permissions;
        }

        /// <summary>
        /// Default to read unless the user explicitely has no read access to an asset.
        /// </summary>
        public bool HasAssetDefaultReadPermission(string type, int id, Permission permission = Permission.ReadAsset)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                var assetTypeID = Query<int>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).FirstOrDefault();
                if (assetTypeID <= 0) return true; // objects not in asset table we grant permission
                hasPermission = Query<int>($"select count(1) from ResponsibilityDetail where ( (Object = @type and ObjectID = @id) or (AssetID = 0 and AssetTypeID = @t) ) and ResourceID = {CurrentResourceID} and PermissionsBitMask & {(int)permission} = 0", new { type, id, t = assetTypeID }).Single() == 0;
            }

            return hasPermission;
        }

        public bool HasAssetPermission(string type, int id, Permission permission)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                int? assetTypeID = null;

                if (type.EndsWith("Type"))
                {
                    assetTypeID = Query<int?>("select ID from AssetType where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
                }
                else
                {
                    assetTypeID = Query<int?>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
                }
                if (assetTypeID.HasValue)
                {
                    hasPermission = Query<bool>($"select cast(IIF(count(1) > 0, 1, 0) as bit) from ResponsibilityDetail where ( (Object = @type and ObjectID = @id) or (AssetID = 0 and AssetTypeID = @t) ) and ResourceID = {CurrentResourceID} and PermissionsBitMask & {(int)permission} = {(int)permission}", new { type, id, t = assetTypeID.Value }).Single();
                }
            }

            return hasPermission;
        }

        public bool HasAssetPermission(long id, Permission permission)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                var assetTypeID = Query<int>("select AssetTypeID from Asset where ID = @id", new { id }).Single();                
                hasPermission = Query<bool>($"select cast(IIF(count(1) > 0, 1, 0) as bit) from ResponsibilityDetail where (AssetID = @id or (AssetID = 0 and AssetTypeID = @t)) and ResourceID = {CurrentResourceID} and PermissionsBitMask & {(int)permission} = {(int)permission}", new { id, t = assetTypeID }).Single();
            }

            return hasPermission;
        }

        public bool HasAssetPermission(SystemObjects type, int id, Permission permission)
        {
            return HasAssetPermission(type.ToString(), id, permission);
        }

        public bool HasAssetTypePermission(string type, int id, Permission permission)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                hasPermission = Query<bool>($"select cast(IIF(count(1) > 0, 1, 0) as bit) from ResponsibilityDetail where [Type] = @type and TypeID = @id and AssetID = 0 and ResourceID = {CurrentResourceID} and PermissionsBitMask & {(int)permission} = {(int)permission}", new { type, id }).Single();
            }

            return hasPermission;
        }

        public bool HasAssetTypePermission(SystemObjects type, int id, Permission permission)
        {
            return HasAssetTypePermission(type.ToString(), id, permission);
        }

        public void RemoveResponsibilityTypeRelation(ResponsibilityTypeRelation relation)
        {
            using (var trans = Database.BeginTransaction())
            {
                try
                {
                    Database.ExecuteSqlCommand(@"
    delete	O 
    from	ResponsibilityTypeRelationOverrideItem O
		    inner join Asset A on A.ID = O.AssetID and O.ResponsibilityTypeID = @ResponsibilityTypeID
		    inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @ObjectType and T.ObjectID = @ObjectID;

    delete	O 
    from	[dbo].[ResponsibilityRuleResultSecurityAsset] O
            inner join ResponsibilityTypeRelationRule R on O.RuleID = R.ID and R.[Object] = @ObjectType and R.[ObjectID] = @ObjectID		    

    delete	O 
    from	[dbo].[ResponsibilityRuleResultAsset] O
            inner join ResponsibilityTypeRelationRule R on O.RuleID = R.ID and R.[Object] = @ObjectType and R.[ObjectID] = @ObjectID

    delete	ResponsibilityTypeRelationRule
    where	ResponsibilityTypeID = @ResponsibilityTypeID
		    and Object = @ObjectType 
		    and ObjectID = @ObjectID;

    delete	ResponsibilityTypeRelation
    where	ResponsibilityTypeID = @ResponsibilityTypeID
		    and ObjectType = @ObjectType 
		    and ObjectID = @ObjectID;", 
            new SqlParameter("@ResponsibilityTypeID", relation.ResponsibilityTypeID),
            new SqlParameter("@ObjectType", relation.ObjectType),
            new SqlParameter("@ObjectID", relation.ObjectID)
            );
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        #endregion
    }
}
