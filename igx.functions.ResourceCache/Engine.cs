using d360.core;
using d360.core.entities;
using d360.utils.company;
using Dapper;
using igx.functions.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace igx.functions.WorkflowScheduling
{
    public static class Generator
    {
        const string functionName = "GenerateResourceCache";
        const string timerSettings = "0 */1 * * * *";
        //const string timerSettings = "*/10 * * * * *";

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log)
        {
            //trigger every two hours: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    try
                    {
                        var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        companyConnection.Open();

                        var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                        cnn.Open();

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

                        companyConnection.Execute(@"merge	reporting.Global_Resource as T
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
                T.IsAdministrator = S.IsAdministrator
when	not matched by target then
		insert (ResourceID, FirstName, LastName, DateLastLoggedIn, Email, Status, IsAdministrator)
		values (S.ResourceID, S.FirstName, S.LastName, S.DateLastLoggedIn, S.Email, S.Status, S.IsAdministrator);",
                            resources.ToArray()
                            );

                        #endregion

                        #region Delete Logic

                        try
                        {
                            var currentResourceIDs = companyConnection.Query<int>("select ResourceID from reporting.Global_Resource").ToList();
                            var updatedResourceIDs = resources.Select(i => i.ResourceID).ToList();
                            currentResourceIDs.ForEach(cr =>
                            {
                                if (!updatedResourceIDs.Contains(cr))
                                {
                                    companyConnection.Execute("delete reporting.Global_Resource where ResourceID = @r", new { r = cr });
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            log.Error($"Error while removing old resources for Company {c.CompanyID}. Error was: {ex.GetFullExceptionData()}");
                        }

                        try
                        {
                            companyConnection.Execute("delete Responsibility where ResponsibleObjectType = 'Resource' and ResponsibleObjectID not in (select ResourceID from reporting.Global_Resource)");
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            log.Error($"Error while removing responsibilities for non-existent resources for Company {c.CompanyID}. Error was: {ex.GetFullExceptionData()}");
                        }

                        #endregion

                        companyConnection.Close();
                        companyConnection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }

                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }
    }
}
