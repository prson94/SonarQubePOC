using d360.core;
using d360.core.entities;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.resourcecache
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public class ResourceCache
    {
        const string functionName = "ResourceCache_Generate";
#if DEBUG
        const string timerSettings = "*/2 * * * * *";
#else
        const string timerSettings = "0 */2 * * * *";
#endif

        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {                
#if DEBUG
                var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 5).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

                using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    foreach (var c in companies)
                    {
                        try
                        {
                            var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                            companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                            #region Get updated resources

                            var resources = cnn.Query<GlobalReportingResource>(@"
select R.ID as ResourceID, 
R.FirstName, 
R.LastName, 
C.LastLoggedInOn, 
R.Email, 
C.[State], 
C.IsAdministrator 
from [Resource] R inner join CompanyResource C on C.ResourceID = R.ID and C.CompanyID = @c", new { c = c.CompanyID }).ToList();

                            #endregion

                            #region Insert/Update Logic

                            using (var transaction = companyConnection.BeginTransaction())
                            {

                                await companyConnection.ExecuteAsync(@"IF OBJECT_ID('tempdb..#users') IS NOT NULL
			                                DROP TABLE #users;

		                                create table #users (                                            			                                
			                                ResourceID int not null primary key ,
                                            FirstName nvarchar(250) not null,
                                            LastName nvarchar(250) not null,
                                            LastLoggedInOn datetime null,
                                            Email nvarchar(500) not null,
                                            [State] int not null,
                                            IsAdministrator bit not null
		                                );
                                ", transaction: transaction);

                                using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.TableLock, transaction))
                                {
                                    bulkCopy.BatchSize = resources.Count;
                                    bulkCopy.DestinationTableName = "#users";
                                    bulkCopy.BulkCopyTimeout = 300;

                                    var table = new DataTable();

                                    var columnName = "ResourceID";
                                    table.Columns.Add(columnName, typeof(int));
                                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                                    columnName = "FirstName";
                                    var dc = table.Columns.Add(columnName, typeof(string));                                    
                                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                                    columnName = "LastName";
                                    table.Columns.Add(columnName, typeof(string));                                    
                                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                                    columnName = "LastLoggedInOn";
                                    table.Columns.Add(columnName, typeof(DateTime));
                                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                                    columnName = "Email";
                                    table.Columns.Add(columnName, typeof(string));
                                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                                    columnName = "State";
                                    table.Columns.Add(columnName, typeof(int));
                                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                                    columnName = "IsAdministrator";
                                    table.Columns.Add(columnName, typeof(bool));
                                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                                    foreach (var item in resources)
                                    {
                                        var row = table.NewRow();

                                        row["ResourceID"] = item.ResourceID;
                                        row["FirstName"] = item.FirstName;
                                        row["LastName"] = item.LastName;
                                        if (item.LastLoggedInOn.HasValue)
                                            row["LastLoggedInOn"] = item.LastLoggedInOn.Value;
                                        else
                                            row["LastLoggedInOn"] = DBNull.Value;

                                        row["Email"] = item.Email;
                                        row["State"] = (int)item.State;
                                        row["IsAdministrator"] = item.IsAdministrator;

                                        table.Rows.Add(row);
                                    }

                                    await bulkCopy.WriteToServerAsync(table);
                                }

                                await companyConnection.ExecuteAsync(@"
merge	reporting.Global_Resource as T
using	(
		select	ResourceID,
				FirstName,
				LastName,
                LastLoggedInOn,
                Email,
                [State],
                IsAdministrator
        from	#users
		) as S
on		(T.ResourceID = S.ResourceID)
when	matched then
		update	
		set		T.FirstName = S.FirstName,
				T.LastName = S.LastName,
                T.LastLoggedInOn = S.LastLoggedInOn,
                T.Email = S.Email,
                T.[State] = S.[State],
                T.IsAdministrator = S.IsAdministrator,
                T.CreatedOn = case when T.CreatedOn is null then getutcdate() else T.CreatedOn end
when	not matched by target then
		insert (ResourceID, FirstName, LastName, LastLoggedInOn, Email, [State], IsAdministrator, CreatedOn)
		values (S.ResourceID, S.FirstName, S.LastName, S.LastLoggedInOn, S.Email, S.[State], S.IsAdministrator, getutcdate());",
                                transaction: transaction,
                                commandTimeout: 300
                                );

                                log.WriteLine("Upserted {0} users for company {1}.", resources.Count, c.CompanyID);


                                transaction.Commit();
                            }

                            #endregion

                            #region Delete Logic

                            try
                            {
                                var currentResourceIDs = companyConnection.Query<int>("select ResourceID from reporting.Global_Resource").ToList();
                                var updatedResourceIDs = resources.Select(i => i.ResourceID).ToList();
                                var deletedCount = 0;
                                currentResourceIDs.ForEach(cr =>
                                {
                                    if (!updatedResourceIDs.Contains(cr))
                                    {
                                        companyConnection.Execute("delete reporting.Global_Resource where ResourceID = @r", new { r = cr });
                                        deletedCount++;
                                    }
                                });

                                if (deletedCount > 0)
                                    log.WriteLine("Removed {0} users for company {1}.", deletedCount, c.CompanyID);
                            }
                            catch (Exception ex)
                            {
                                CoreFunction.AITrackException(functionName, ex, c.CompanyID);                                
                            }

                            try
                            {
                                companyConnection.Execute("delete ResponsibilityTypeRelationOverrideItem where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
                                companyConnection.Execute("delete [dbo].[ResponsibilityRuleResultSecurityAsset] where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
                            }
                            catch (Exception ex)
                            {
                                CoreFunction.AITrackException(functionName, ex, c.CompanyID);                                
                            }

                            #endregion

                            companyConnection.Close();
                            companyConnection.Dispose();
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }
    }
}
