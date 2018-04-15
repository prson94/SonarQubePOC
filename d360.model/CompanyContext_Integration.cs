using d360.core;
using d360.core.entities;
using Dapper;
using System;
using System.Data.Entity;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<IntegrationSetting> IntegrationSettings { get; set; }

        public DbSet<IntegrationAssetType> IntegrationAssetTypes { get; set; }

        public DbSet<IntegrationAssetTypeFieldItem> IntegrationAssetTypeFieldItems { get; set; }

        public DbSet<IntegrationAssetTypeRelationItem> IntegrationAssetTypeRelationItems { get; set; }

        public DbSet<IntegrationAssetTypeRelationItemTarget> IntegrationAssetTypeRelationItemTargets { get; set; }

        public DbSet<IntegrationAssetTypeRoleItem> IntegrationAssetTypeRoleItems { get; set; }

        #endregion

        #region Engine Methods

        #endregion
    }
}
