using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model
{
	public partial interface ICompanyContext : IBaseContext 
	{
		#region DbSets

		DbSet<GlobalReportingResource> GlobalReportingResources { get; set; }

		DbSet<Group> Groups { get; set; }

		DbSet<ResourceGroup> ResourceGroups { get; set; }

		DbSet<ResourcePasswordReset> ResourcePasswordResets { get; set; }

		DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }

		DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }

		DbSet<ResponsibilityTypeRelationRule> ResponsibilityTypeRelationRules { get; set; }

		DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

		DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

		#endregion

		#region Methods

		string GetNoReadSqlStatement(string identifier = null);

		string GetNoReadSqlStatement(Permission permission, string identifier = null);

		string GetAssetTypeNoReadSqlStatement(string identifier = null);

		string GetAssetTypeNoReadSqlStatement(Permission permission, string identifier = null);

		List<PermissionInfo> GetTypePermissions(string type, int typeID);

		/// <summary>
		/// Derives from SQL Function dbo.UserAssetPermissions which performs slower as we cannot utilize some sql optimizations on sql functions.
		/// </summary>
		/// <param name="tempTableName"></param>
		/// <param name="userParam"></param>
		/// <param name="typeParam"></param>
		/// <returns></returns>
		string GetUserPermissionQuery(string tempTableName, string userParam, string typeParam);

		bool HasAssetPermission(long id, Permission permission);

		bool HasAssetPermission(string type, int id, Permission permission);

		bool HasAssetPermission(SystemObjects type, int id, Permission permission);

		bool HasAssetPermissionByUid(Guid uid, Permission permission);

		/// <summary>
		/// Used to get if a user has read permissions on a given item.  Read is assumed to be present unless denied.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="objectId"></param>
		/// <param name="assetTypeId"></param>
		/// <param name="permission"></param>
		/// <param name="permission"></param>
		/// <returns></returns>
		bool HasUserReadPermission(string type, int objectId, int assetTypeId, int resourceId);

		bool HasAssetTypePermission(string type, int id, Permission permission);

		bool HasAssetTypePermission(SystemObjects type, int id, Permission permission);

		bool HasAssetTypePermission(int id, Permission permission);

		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		#region DbSets

		public DbSet<GlobalReportingResource> GlobalReportingResources { get; set; }

		public DbSet<Group> Groups { get; set; }

		public DbSet<ResourceGroup> ResourceGroups { get; set; }

		public DbSet<ResourcePasswordReset> ResourcePasswordResets { get; set; }

		public DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }                              /* VIEW */

		public DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

		public DbSet<ResponsibilityTypeRelationOverrideItem> ResponsibilityTypeRelationOverrideItems { get; set; }

		public DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

		public DbSet<ResponsibilityTypeRelationRule> ResponsibilityTypeRelationRules { get; set; }

		#endregion

		#region Utility

		/// <summary>
		/// Default to read unless the user explicitly has no read access to an asset.
		/// </summary>
		private bool HasAssetDefaultReadPermission(string type, int id)
		{
			bool hasPermission = SecurityContext.IsAdministrator;
			if (!hasPermission)
			{
				int assetTypeID = Query<int>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).FirstOrDefault();

				if (assetTypeID <= 0)
				{
					return true; // objects not in asset table we grant permission               
				}

				hasPermission = HasReadPermission(type, id, assetTypeID);
			}

			return hasPermission;
		}

		/// <summary>
		/// Used to determine if a user has read permissions on a given asset type.  Read is assumed to be present unless denied.
		/// </summary>        
		/// <param name="assetTypeId"></param>        
		/// <returns></returns>
		private bool HasAssetTypeReadPermission(int assetTypeId)
		{
			Permission permission = Permission.ReadAsset;

			return Database.Connection.QuerySingle<bool>($@"
declare	@assetTypePermissions int, 
		@hasAssetTypePermission bit = 0

select	@assetTypePermissions = dbo.GetCombinedPermissionsForUserByAssetTypeId(@t, @r);
set		@hasAssetTypePermission = @assetTypePermissions & {(int)permission}

select  @hasAssetTypePermission", new { t = assetTypeId, r = SecurityContext.ResourceID });
		}

		private bool HasPermission(long assetId, int assetTypeId, Permission permission)
		{
			bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);
			if (isReadPermission)
			{
				permission = Permission.ReadAsset;
			}

			return Database.Connection.QuerySingle<bool>($@"
declare	@assetPermissions int
select	@assetPermissions = dbo.GetCombinedPermissionsForUserByAssetId(@assetId, @r);
select  (@assetPermissions & {(int)permission})
", new { assetId, r = SecurityContext.ResourceID });
		}

		private bool HasPermission(string type, int objectId, int assetTypeId, Permission permission)
		{
			return Database.Connection.QuerySingle<bool>(
				$"declare	@assetPermissions int;" +
				$"select	@assetPermissions = dbo.GetCombinedPermissionsForUserByAssetLegacy(@type, @objectId, @r);" +
				$"select	(@assetPermissions & {(int)permission})", 
				new { type, objectId, r = SecurityContext.ResourceID });
		}

		/// <summary>
		/// Used to get if a user has read permissions on a given item.  Read is assumed to be present unless denied.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="objectId"></param>
		/// <param name="assetTypeId"></param>
		/// <returns></returns>
		private bool HasReadPermission(string type, int objectId, int assetTypeId)
		{
			return HasUserReadPermission(type, objectId, assetTypeId, SecurityContext.ResourceID);
		}

		#endregion

		#region Methods

		public string GetNoReadSqlStatement(string identifier = null)
		{
			return GetNoReadSqlStatement(Permission.ReadAsset, identifier);
		}

		public string GetAssetTypeNoReadSqlStatement(string identifier = null)
		{
			return GetAssetTypeNoReadSqlStatement(Permission.ReadAsset, identifier);
		}

		public string GetNoReadSqlStatement(Permission permission, string identifier = null)
		{
			return $"select AssetID from ResponsibilityDetail where ((PermissionsBitMask & {(int)permission}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? SecurityContext.ResourceID.ToString() : identifier)}";
		}

		public string GetAssetTypeNoReadSqlStatement(Permission permission, string identifier = null)
		{
			return $"select AssetTypeID from ResponsibilityDetail where AssetID = 0 and ((PermissionsBitMask & {(int)permission}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? SecurityContext.ResourceID.ToString() : identifier)}";
		}

		public List<PermissionInfo> GetTypePermissions(string type, int typeID)
		{
			List<PermissionInfo> permissions = Permission.DeleteAsset.GetList();

			string qry = $@"
							declare @AssetTypeID int;
							select @AssetTypeID = ID
							from AssetType 
							where Object = @type
							and ObjectID = @typeID;

							select distinct R.PermissionsBitMask
							from [dbo].ResponsibilityDetailByAssetTypeIDAssetID(@AssetTypeID,0) R
							where ResourceID = @ResourceID;
							";

			List<int> responsibilityAssignments = Query<int>(qry, new { type, typeID, SecurityContext.ResourceID }).ToList();

			permissions.ForEach(p =>
			{
				p.Selected = responsibilityAssignments.Any(i => (i & p.Value) == p.Value);
			});

			//AddAsset no longer requires ApplyToType (GOV-13993). But because the ResponsibilityDetail view relies on the ...RuleResults tables,
			//we will need to check for this permission with HasAssetTypePermission() if it has not already been selected
			permissions
				.Where(p => p.Value == (int)Permission.AddAsset && !p.Selected)
				.ToList()
				.ForEach(p => p.Selected = HasAssetTypePermission(type, typeID, Permission.AddAsset)
			);

			permissions.RemoveAll(i => !i.Selected);

			return permissions;
		}

		public List<PermissionInfo> GetPermissions(long assetId, int assetTypeId)
		{
			List<PermissionInfo> permissions = Permission.DeleteAsset.GetList();
			int mask = 0;

			//if (SecurityContext.IsAdministrator)
			//{
			//	mask = 15854;
			//}

			mask = Query<int>("select dbo.GetCombinedPermissionsForUserByAssetId(@assetId, @r)", new { r = SecurityContext.ResourceID, assetId }).First();
			Permission CombinedPermission = (Permission)mask;

			permissions.ForEach(p =>
			{
				p.Selected = (CombinedPermission & p.ID) == p.ID;
			});

			permissions.RemoveAll(i => !i.Selected);

			return permissions;
		}

		public string GetCheckPermissionResult(int PermissionMask, int perm)
		{
			return ((uint)PermissionMask & (uint)perm) == (uint)perm ? "True" : "False";
		}

		public bool GetPermissionsRead(long assetId, int assetTypeId)
		{
			var perms = GetPermissions(assetId, assetTypeId);
			return perms.Any(p => p.ID == Permission.ReadAsset);
		}

		public string GetUserPermissionQuery(string tempTableName = "PermissiondAssets", string userParam = "ResourceID", string typeParam = "AssetTypeID")
		{
			return $@"
drop table if exists #{tempTableName};
create table #{tempTableName}(
	AssetId int,
	AssetTypeID bigint,
	PermissionsBitMask int
);

insert into #{tempTableName} (PermissionsBitMask, AssetId, AssetTypeID)
	select	[Permissions],
			AssetID,
			AssetTypeID
	from	ResponsibilitySummary
	where	AssetTypeID = cast(@{typeParam} as int)
			and ResourceID = cast(@{userParam} as int);";
		}

		public bool HasAssetPermission(string type, int id, Permission permission)
		{
			bool hasPermission = SecurityContext.IsAdministrator;

			if (!hasPermission)
			{

				if (permission == Permission.ReadAsset)
				{
					hasPermission = HasAssetDefaultReadPermission(type, id);
				}
				else
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
						hasPermission = HasPermission(type, id, assetTypeID.Value, permission);
					}
				}
			}

			return hasPermission;
		}

		public bool HasUserReadPermission(string type, int objectId, int assetTypeId, int resourceId)
		{
			Permission permission = Permission.ReadAsset;

			return Database.Connection.QuerySingle<bool>(
				"declare	@assetPermissions int;" +
				"select		@assetPermissions = dbo.GetCombinedPermissionsForUserByAssetLegacy(@type, @objectId, @r);" +
				$"select	(@assetPermissions & {(int)permission})",
				new { type, objectId, r = resourceId });
		}

		public bool HasAssetPermission(long id, Permission permission)
		{
			bool hasPermission = SecurityContext.IsAdministrator;

			if (!hasPermission)
			{
				int assetTypeID = Query<int>("select AssetTypeID from Asset where ID = @id", new { id }).Single();
				hasPermission = HasPermission(id, assetTypeID, permission);
			}

			return hasPermission;
		}

		public bool HasAssetPermissionByUid(Guid uid, Permission permission)
		{
			bool hasPermission = SecurityContext.IsAdministrator;

			if (!hasPermission)
			{
				Asset asset = Assets.Single(a => a.uid == uid);
				hasPermission = HasPermission(asset.ID, asset.AssetTypeID, permission);
			}

			return hasPermission;
		}

		public bool HasAssetPermission(SystemObjects type, int id, Permission permission)
		{
			return HasAssetPermission(type.ToString(), id, permission);
		}

		public bool HasAssetTypePermission(string type, int id, Permission permission)
		{
			bool hasPermission = SecurityContext.IsAdministrator;
			bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);


			if (!hasPermission)
			{
				if (isReadPermission)
				{
					hasPermission = HasAssetTypeReadPermission(id);
				}
				else
				{
					hasPermission = Database.Connection.QuerySingle<bool>($@"
																			declare @t int;
																			select @t = ID from AssetType where [Object] = @type and [ObjectID] = @id;

																			if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
																				begin
																					select 1;
																				end				                                                                        
																			else
																				begin
																					select 0;
																				end", new { id, type, r = SecurityContext.ResourceID });
				}
			}

			return hasPermission;
		}

		public bool HasAssetTypePermission(SystemObjects type, int id, Permission permission)
		{
			return HasAssetTypePermission(type.ToString(), id, permission);
		}

		public bool HasAssetTypePermission(int assetTypeId, Permission permission)
		{
			AssetType assetType = Query<AssetType>("select * from AssetType where ID = @id", new { id = assetTypeId }).Single();

			return HasAssetTypePermission(assetType.Object, assetTypeId, permission);
		}
        
		#endregion
	}
}
