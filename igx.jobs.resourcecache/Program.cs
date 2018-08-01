using d360.core;
using d360.core.entities;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace igx.jobs.resourcecache
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();

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
        const string timerSettings = "0 */1 * * * *";
#endif

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                //CoreFunction.AITrackJobStart(functionName);
#if DEBUG
                var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 4).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

                var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                companies.ForEach(c =>
                {
                    try
                    {
                        var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        #region Get updated resources

                        var resources = cnn.Query<GlobalReportingResource>(@"
select ID as ResourceID, FirstName, 
LastName, 
DateLastLoggedIn, 
Email, 
Status, 
IsAdministrator 
from Resource R 
inner join CompanyResource C on C.ResourceID = R.ID and C.CompanyID = @c", new { c = c.CompanyID }).ToList();

                        #endregion

                        #region Insert/Update Logic

                        companyConnection.Execute(@"
merge	reporting.Global_Resource as T
using	(
		select	@ResourceID as ResourceID,
				@FirstName as FirstName,
				@LastName as LastName,
                @DateLastLoggedIn as DateLastLoggedIn,
                @Email as Email,
                @Status as Status,
                @IsAdministrator as IsAdministrator
		) as S
on		(T.ResourceID = S.ResourceID)
when	matched then
		update	
		set		T.FirstName = S.FirstName,
				T.LastName = S.LastName,
                T.DateLastLoggedIn = S.DateLastLoggedIn,
                T.Email = S.Email,
                T.Status = S.Status,
                T.IsAdministrator = S.IsAdministrator,
                T.CreatedOn = case when T.CreatedOn is null then getutcdate() else T.CreatedOn end
when	not matched by target then
		insert (ResourceID, FirstName, LastName, DateLastLoggedIn, Email, Status, IsAdministrator, CreatedOn)
		values (S.ResourceID, S.FirstName, S.LastName, S.DateLastLoggedIn, S.Email, S.Status, S.IsAdministrator, getutcdate());",
                            resources.ToArray()
                            );

                        log.WriteLine("Upserted {0} users for company {1}.", resources.Count, c.CompanyID);

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
                            //log.Error($"Error while removing old resources for Company {c.CompanyID}. Error was: {ex.GetFullExceptionData()}");
                        }

                        try
                        {
                            companyConnection.Execute("delete ResponsibilityTypeRelationOverrideItem where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
                            companyConnection.Execute("delete ResponsibilityTypeRelationRuleResult where SecurityAsset = 'R' and SecurityAssetID not in (select ResourceID from reporting.Global_Resource)");
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            //log.Error($"Error while removing responsibilities for non-existent resources for Company {c.CompanyID}. Error was: {ex.GetFullExceptionData()}");
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

                });

                cnn.Close();
                cnn.Dispose();
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
