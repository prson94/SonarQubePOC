using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Dapper;
using d360.core.entities;
using System.Data.SqlClient;
using d360.core;

namespace d360.jobs.RefreshResourceCache
{
    public class Functions : FunctionsBase
    {
        public static List<Exception> Generate()
        {
            var mex = new List<Exception>();
            
            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 11).ToList();

                companies.AsParallel().ForAll(companyID =>
                {
                    try
                    {
                        Console.WriteLine("Starting Resource Synchronization for Company {0}", companyID);

                        var companyConnection = GetCompanyConnection(companyID);
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
inner join CompanyResource C on C.ResourceID = R.ID and C.CompanyID = @c", new { c = companyID }).ToList();

                        #endregion

                        Console.WriteLine("Synchronizing {0} resources for Company {1}", resources.Count, companyID);

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
                            Console.WriteLine("Error while removing old resources for Company {0}. Error was: {1}", companyID, ex.GetFullExceptionData());
                        }

                        try
                        {
                            companyConnection.Execute("delete Responsibility where ResponsibleObjectType = 'Resource' and ResponsibleObjectID not in (select ResourceID from reporting.Global_Resource)");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error while removing responsibilities for non-existent resources for Company {0}. Error was: {1}", companyID, ex.GetFullExceptionData());
                        }

                        #endregion

                        companyConnection.Close();
                        companyConnection.Dispose();

                        Console.WriteLine("Completing Resource Synchronization for Company {0}", companyID);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while processing Company {0}. Error was: {1}", companyID, ex.GetFullExceptionData()); 
                    }
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                Trace.TraceError(msg);
            }

            return mex;
        }
    }
}
