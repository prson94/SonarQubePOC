using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Dapper;
using d360.workflow;
using d360.workflow.models;
using d360.core;

namespace d360.jobs.TriggerArtifactCertificationWorkflow
{
    public class CertificationProcedureModel
    {
        public int ArtifactID { get; set; }
        public DateTime CertificationStartDate { get; set; }
        public DateTime CertificationEndDate { get; set; }
    }

    public class UserToEmailModel
    {
        public Guid WorkflowID { get; set; }
        public short Activity { get; set; }
        public int ResourceID { get; set; }
        public bool IsComplete { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class Functions : FunctionsBase
    {
        public static List<Exception> CallDatabase()
        {
            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 1).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.ForEach(companyID =>
                {
                    try
                    {
                        var companyConnection = GetCompanyConnection(companyID);
                        companyConnection.Open();

                        var domainPrefix = domainPrefixes[companyID];

                        var processor = new Processor();
                        var models = companyConnection.Query<CertificationProcedureModel>("exec utility.GetArtifactsUpForCertification").ToList();

                        Console.WriteLine("Company: {0}. Processing Artifacts Up For Certification.  {1} items up for certification.", companyID, models.Count);

                        models.ForEach(m =>
                        {
                            try
                            {
                                var dictionary = new Dictionary<string, object>();
                                dictionary.Add("CompanyID", companyID);
                                dictionary.Add("requestInfo", new CertifyArtifactRequest { ArtifactID = m.ArtifactID, DueDate = m.CertificationEndDate, StartDate = m.CertificationStartDate });

                                processor.CreateNewWorkflowInstance(WorkflowVersionMap.CertifyArtifactIdentity_vCurrent, dictionary);

                                companyConnection.Execute("update Artifact set Status = 'Under Review' where ID = @id", new { id = m.ArtifactID });
                            }
                            catch (Exception ex)
                            {
                                mex.Add(ex);
                            }
                        });

                        var usersToEmail = companyConnection.Query<UserToEmailModel>(
@"select		WR.*,
			R.Email,
			R.FirstName + ' ' + R.LastName as Name,
			W.Data.value('(/fields/DueDate)[1]', 'datetime') as DueDate
from	    Workflow W
			inner join WorkflowResource WR on	WR.WorkflowID = W.ID 
												and W.DateCompleted is null
												and WR.IsComplete = 0
												and W.WorkflowType = 2
			left join WorkflowResourceNotification WRN on WRN.WorkflowID = WR.WorkflowID 
					and WRN.Activity = WR.Activity 
					and WRN.ResourceID = WR.ResourceID 
					and WRN.[Date] = W.Data.value('(/fields/DueDate)[1]', 'datetime')
			inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID
			inner join Artifact A on A.ID = W.Data.value('(/fields/ArtifactID)[1]', 'int') and A.Status <> 'Archived'
			inner join ArtifactType T on T.ID = A.ArtifactTypeID
where		WRN.[Date] is null").ToList();

                        var list = usersToEmail
                            .Select(i => new { i.Name, i.Email, i.DueDate, i.ResourceID, i.Activity })
                            .GroupBy(i => new { i.Name, i.Email, i.DueDate, i.ResourceID, i.Activity })
                            .Select(i => new { Info = i.Key, Count = i.Count() })
                            .ToList();

                        Console.WriteLine("Company: {0}. Sending Notifications For Certification.  Found {1} users to notify.", companyID, list.Count);

                        foreach (var userToEmail in list)
                        {
                            var tags = new Dictionary<string, string>();
                            tags.Add("user", userToEmail.Info.Name);
                            tags.Add("count", userToEmail.Count.ToString());
                            tags.Add("appUrl", string.Format("https://{0}.data3sixty.com", domainPrefix));
                            tags.Add("dueDate", userToEmail.Info.DueDate.Value.ToShortDateString());
                            SendMailToUser(userToEmail.Info.Name, userToEmail.Info.Email, "Data3Sixty - Time to Certify", "", "certify-artifacts-request", tags);
 
                            foreach (var wr in usersToEmail.Where(i => i.Activity == userToEmail.Info.Activity 
                                && i.ResourceID == userToEmail.Info.ResourceID 
                                && i.DueDate == userToEmail.Info.DueDate)
                                )
                            {
                                try
                                {
                                    companyConnection.Execute(@"insert into WorkflowResourceNotification (WorkflowID, Activity, ResourceID, [Date]) values (@w, @a, @r, @d)",
                                        new { w = wr.WorkflowID, a = wr.Activity, r = wr.ResourceID, d = wr.DueDate });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Company: {0}. Error occured when trying to save WorkflowResourceNotification. Error is: {1}.", companyID, ex.GetFullExceptionData());
                                }                                
                            }
                        }                        
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                        Trace.TraceError(msg);
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
