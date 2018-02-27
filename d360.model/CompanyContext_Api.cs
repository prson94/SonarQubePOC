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

        public DbSet<ApiEntityUri> ApiEntityUris { get; set; }

        #endregion

        #region Engine Methods

        //public int AddWebStatistic(SystemObjects @object, int objectID, string ip, string userAgent, string host, string browserLanguage, string action, int resourceID, DateTime timestamp)
        //{
        //    return Database.Connection
        //        .Execute(@"analytics.AddStatistic @Object, @ObjectID, @Ip, @UserAgent, @Host, @BrowserLanguage, @Action, @ResourceID, @Timestamp",
        //        new
        //        {
        //            Object = new Dapper.DbString { Value = @object.ToString(), IsAnsi = false, IsFixedLength = false, Length = 50 },
        //            ObjectID = objectID,
        //            UserAgent = userAgent,
        //            Ip = new Dapper.DbString { Value = ip, IsAnsi = false, IsFixedLength = false, Length = 100 },
        //            Host = new Dapper.DbString { Value = host, IsAnsi = false, IsFixedLength = false, Length = 50 },
        //            BrowserLanguage = new Dapper.DbString { Value = browserLanguage, IsAnsi = false, IsFixedLength = false, Length = 500 },
        //            Action = new Dapper.DbString { Value = action, IsAnsi = false, IsFixedLength = false, Length = 50 },
        //            ResourceID = resourceID,
        //            Timestamp = timestamp
        //        });
        //}

        #endregion
    }
}
