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

        public DbSet<ApiService> ApiServices { get; set; }

        public DbSet<ApiEndpoint> ApiEndpoints { get; set; }

        public DbSet<ApiEndpointVersion> ApiEndpointVersions { get; set; }

        public DbSet<ApiEntity> ApiEntities { get; set; }

        public DbSet<ApiEntityFieldType> ApiEntityFieldTypes { get; set; }

        public DbSet<ApiEntityFieldTypeMultiSelectField> ApiEntityFieldTypeMultiSelectFields { get; set; }

        public DbSet<ApiEntityUri> ApiEntityUris { get; set; }

        public DbSet<ApiExecution> ApiExecutions { get; set; }

        public DbSet<ApiNamespace> ApiNamespaces { get; set; }

        #endregion                
    }
}
