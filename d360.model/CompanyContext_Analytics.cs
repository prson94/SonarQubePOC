using d360.core;
using d360.core.entities.Workflow;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.model.workflow;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        //public DbSet<WorkflowEventRegistration> StatisticDetails { get; set; }

        #endregion

        #region Engine Methods

        public int AddWebStatistic(int companyID, SystemObjects @object, int objectID, string ip, string userAgent, string host, string browserLanguage, string action, int resourceID, DateTime timestamp)
        {
            return Database.Connection
                .Execute(@"analytics.AddStatistic @Object, @ObjectID, @Ip, @UserAgent, @Host, @BrowserLanguage, @Action, @ResourceID, @Timestamp",
                new
                {
                    Object = new Dapper.DbString { Value = @object.ToString(), IsAnsi = false, IsFixedLength = false, Length = 50 },
                    ObjectID = objectID,
                    UserAgent = userAgent,
                    Ip = new Dapper.DbString { Value = ip, IsAnsi = false, IsFixedLength = false, Length = 100 },
                    Host = new Dapper.DbString { Value = host, IsAnsi = false, IsFixedLength = false, Length = 50 },
                    BrowserLanguage = new Dapper.DbString { Value = browserLanguage, IsAnsi = false, IsFixedLength = false, Length = 500 },
                    Action = new Dapper.DbString { Value = action, IsAnsi = false, IsFixedLength = false, Length = 50 },
                    ResourceID = resourceID,
                    Timestamp = timestamp
                });
        }

        #endregion
    }
}
