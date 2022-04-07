using System;
using System.Data.Entity;

using d360.core;

using Dapper;

namespace d360.model
{
    public partial class CompanyContext : BaseContext
    {
        public int AddWebStatistic(SystemObjects @object, int objectID, string ip, string userAgent, string host, string browserLanguage, string action, int resourceID, DateTime timestamp)
        {
            return Database.Connection
                .Execute(@"analytics.AddStatistic @Object, @ObjectID, @Ip, @UserAgent, @Host, @BrowserLanguage, @Action, @ResourceID, @Timestamp",
                new
                {
                    Object = new DbString { Value = @object.ToString(), IsAnsi = false, IsFixedLength = false, Length = 50 },
                    ObjectID = objectID,
                    UserAgent = userAgent,
                    Ip = new DbString { Value = ip, IsAnsi = false, IsFixedLength = false, Length = 100 },
                    Host = new DbString { Value = host, IsAnsi = false, IsFixedLength = false, Length = 50 },
                    BrowserLanguage = new DbString { Value = browserLanguage, IsAnsi = false, IsFixedLength = false, Length = 500 },
                    Action = new DbString { Value = action, IsAnsi = false, IsFixedLength = false, Length = 50 },
                    ResourceID = resourceID,
                    Timestamp = timestamp
                });
        }
    }
}
