using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using d360.core;

namespace d360.jobs.QualityChecks
{
    public class TextPathModel
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public string TextPath { get; set; }
        public string CorrectTextPath { get; set; }
    }
    class Program: FunctionsBase
    {
        private static int _defaultQueryCommandTimeout = 180;

        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();
                //var companies = GetActiveDevelopmentCompanyIDs();

#if DEBUG                       
                companies = GetActiveCompanyIDs().Where(i => i == 4).ToList();
#endif
           
              companies.ForEach(companyID =>
              {
                  try
                  {
                      using (var context = GetCompanyConnection(companyID))
                      {
                          Console.WriteLine("Getting objects with invalid text paths [company id: {0}]", companyID);

                          context.OpenWithRetry(RetryPolicy.DefaultFixed);
                          var items = context.Query<TextPathModel>(@"
select  od.[Object],
		od.[ObjectID],
		od.[TextPath],
		utility.GetBreadcrumbStringWrapper(od.[object], od.[objectid], '/') as CorrectTextPath
from    cache.objectdetails od
where   od.[textpath] != utility.GetBreadcrumbStringWrapper(od.[object], od.[objectid], '/') and
		od.[object] in ('Artifact','Taxonomy', 'Policy')").ToList();

                          Console.WriteLine("Found {0} item(s) with invalid text paths [company id: {1}]", items.Count, companyID);

                          items.ForEach(i => {
                              try
                              {
                                  context.Execute($"update {i.Object} set TextPath = @tp where ID = @id", new { tp = i.CorrectTextPath, id = i.ObjectID });
                              }
                              catch (Exception ex)
                              {
                                  Console.WriteLine(ex.GetFullExceptionData());
                              }
                          });
                      }
                  }
                  catch (Exception ex)
                  {
                      Console.WriteLine(ex.GetFullExceptionData());
                  }

              });
            }
            catch (Exception ex)
            {
                mex.Add(ex);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
