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

        public DbSet<ResponsibilityDetailForResource> ResponsibilityDetailForResources { get; set; }        /* VIEW */

        public DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }                              /* VIEW */

        public DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

        public DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }

        public DbSet<ResponsibilityTypeClaim> ResponsibilityTypeClaims { get; set; }

        public DbSet<ResponsibilityTypeObjectClaim> ResponsibilityTypeObjectClaims { get; set; }

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

        public IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByAssetType(int id)
        {
            try
            {
                return Database.Connection.Query<ResponsibilityType>(@"
select	RT.*
from	ResponsibilityType RT
		inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeID = RT.ID
		inner join AssetType T on T.Object = R.ObjectType and T.ObjectID = R.ObjectID and T.ID = @id 
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

        public IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByObject(SystemObjects type, int id)
        {
            try
            {
                return Database.Connection.Query<ResponsibilityType>("EXEC GetAllowedResponsibilityTypesByObject @type, @id", new
                {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id = id
                }).AsQueryable();
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

        public IQueryable<SecurityDetail> GetPermissions(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Filter<SecurityDetail>(i => i.ObjectType == sType && i.ObjectID == id && i.ResponsibleObjectID == CurrentResourceID);
        }

        public IQueryable<SecurityDetail> GetPermissions(SystemObjects type, int[] id)
        {
            var sType = type.ToString();
            return Filter<SecurityDetail>(i => i.ObjectType == sType && id.Contains(i.ObjectID) && i.ResponsibleObjectID == CurrentResourceID);
        }

        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByObject(SystemObjects type, int id)
        {
            try
            {
                var sType = type.ToString();
                return Filter<ResponsibilityDetail>(i => i.Object == sType && i.ObjectID == id);
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility");
            }
            catch
            {
                throw;
            }
        }

        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByResource(SystemObjects type, int id)
        {
            try
            {
                return Filter<ResponsibilityDetail>(i => i.ResourceID == id);
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility");
            }
            catch
            {
                throw;
            }
        }

        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByType(int id)
        {
            try
            {
                return Filter<ResponsibilityDetail>(i => i.ResponsibilityTypeID == id);
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility");
            }
            catch
            {
                throw;
            }
        }

        public bool HasClaimInCurrentPermissionList(List<SecurityDetail> list, Claim claim, ClaimObject claimObject = ClaimObject.Root)
        {
            var has = CurrentResourceIsAdmin;
            if (!has) has = list.Any(i => i.Claim == claim && i.ClaimObject == claimObject);
            return has;
        }

        public bool HasPermission(SystemObjects type, int id, Claim claim, ClaimObject claimObject = ClaimObject.Root)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                var sType = type.ToString();
                hasPermission = Any<SecurityDetail>(i => i.ObjectType == sType && i.ObjectID == id && i.ResponsibleObjectID == CurrentResourceID && i.Claim == claim && i.ClaimObject == claimObject);
            }

            return hasPermission;
        }

        #endregion
    }
}
