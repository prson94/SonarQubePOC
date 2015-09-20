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
using System.Data.SqlClient;
using Dapper;
using Microsoft.WindowsAzure;

namespace d360.scheduler.jobs
{
    [DisallowConcurrentExecution()]
    public class CalculateStatisticsJob : BaseJob, IJob, IScheduledJob
    {
        public override int IntervalInMinutes { get { return 1; } }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var cnn = new SqlConnection(CloudConfigurationManager.GetSetting("CommunityContext"));
                cnn.Open();
                var companies = cnn.Query<Company>(@"select * from Company").ToList();
                cnn.Close();
                cnn.Dispose();

                companies.ForEach(c => { 
                
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Trace.TraceError(msg);
                throw new JobExecutionException(msg, ex, false);
            }
        }
    }
}
