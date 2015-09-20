using d360.core;
using d360.core.entities;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Quartz;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using d360.model;
using System.Configuration;
using Lucene.Net.Store.Azure;
using Autofac;
using d360.extensions;
using System.Reflection;
using System.Diagnostics;

namespace d360.scheduler.jobs
{
    [DisallowConcurrentExecution()]
    public class CacheSecurityJob: BaseJob, IJob, IScheduledJob
    {
        public override bool Enabled { get { return GetEnabledFromConfig(this.GetType().Name); } }
        public override int IntervalInMinutes { get { return GetIntervalFromConfig(this.GetType().Name); } }
        public override string JobName { get { return "Cache Security"; } }


        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Trace.TraceInformation(START_JOB_MESSAGE, JobName);

                //if (StartJob())
                //{
                    //bool anyFailed = false;

                    ctx.Companies.ToList().AsParallel().WithDegreeOfParallelism(4).ForAll(c =>
                    {
                        try
                        {
                            RunForCompany(c);
                        }
                        catch
                        {
                            //anyFailed = true;
                        }
                    });

                //    StopJob(anyFailed ? "ERROR: See event log" : "OK");
                //}
            }
            catch (Exception ex)
            {
                var msg = string.Format(JOB_ERROR_MESSAGE, JobName, ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : ""));
                Trace.TraceError(msg);
                throw new JobExecutionException(msg, ex, false);
            }
            finally 
            {
                Trace.TraceInformation(STOP_JOB_MESSAGE, JobName);
            }
        }

        void RunForCompany(Company c)
        {
            var companyCtx = new D360Context(c.ID, 0, true);

            try
            {
                Trace.TraceInformation(CONNECTING_TO_COMPANY, c.Name, JobName);
                companyCtx.ExecuteCompanyFederationCommand();
                companyCtx.CacheSecurity();
                Trace.TraceInformation(SUCCESS_COMPLETE_MESSAGE, JobName, c.Name);
            }
            catch (Exception ex)
            {
                Trace.TraceError(JOB_COMPANY_ERROR_MESSAGE, JobName, c.Name, ex.Message);
                throw ex;
            }
            finally 
            {
                companyCtx.Dispose();
            }
        }
    }
}
