using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using Dapper;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

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

        public List<PermissionInfo> GetPermissions(string type, int typeID, string @object, int objectID)
        {
            var permissions = Permission.DeleteAsset.GetList();

            var sType = type.ToString();

            var responsibilityAssignments = Filter<ResponsibilityDetail>(i => 
            (
                (i.Object == @object && i.ObjectID == objectID) ||
                (i.Type == type && i.TypeID == typeID)
            )
            && i.ResourceID == CurrentResourceID).Select(i => i.PermissionsBitMask).Distinct().ToList();

            permissions.ForEach(p =>
            {
                p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
            });

            permissions.RemoveAll(i => !i.Selected);

            return permissions;
        }

        public List<PermissionInfo> GetPermissions(SystemObjects type, int id)
        {
            var permissions = Permission.DeleteAsset.GetList();

            var sType = type.ToString();

            var responsibilityAssignments = Filter<ResponsibilityDetail>(i => i.Object == sType && i.ObjectID == id && i.ResourceID == CurrentResourceID).Select(i => i.PermissionsBitMask).Distinct().ToList();

            permissions.ForEach(p =>
            {
                p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
            });

            permissions.RemoveAll(i => !i.Selected);

            return permissions;
        }

        public bool HasAssetPermission(string type, int id, Permission permission)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                hasPermission = Query<bool>($"select cast(IIF(count(1) > 0, 1, 0) as bit) from ResponsibilityDetail where Object = @type and ObjectID = @id and ResourceID = {CurrentResourceID} and PermissionsBitMask & {(int)permission} = {(int)permission}", new { type, id }).Single();
            }

            return hasPermission;
        }

        public bool HasAssetPermission(long id, Permission permission)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                hasPermission = Query<bool>($"select cast(IIF(count(1) > 0, 1, 0) as bit) from ResponsibilityDetail where AssetID = @id and ResourceID = {CurrentResourceID} and PermissionsBitMask & {(int)permission} = {(int)permission}", new { id }).Single();
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
                hasPermission = Query<bool>($"select cast(IIF(count(1) > 0, 1, 0) as bit) from ResponsibilityDetail where [Type] = @type and TypeID = @id and ResourceID = {CurrentResourceID} and PermissionsBitMask & {(int)permission} = {(int)permission}", new { type, id }).Single();
            }

            return hasPermission;
        }

        public bool HasAssetTypePermission(SystemObjects type, int id, Permission permission)
        {
            return HasAssetPermission(type.ToString(), id, permission);
        }

        #endregion
    }
}
