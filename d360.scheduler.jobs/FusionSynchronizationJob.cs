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
using Autofac.Features.Metadata;
using System.Net;

namespace d360.scheduler.jobs
{
    public class FusionProcessor
    {
        readonly IEnumerable<Meta<IFusionSynchronizationSource>> Items;

        public FusionProcessor(IEnumerable<Meta<IFusionSynchronizationSource>> items)
        {
            Items = items;
        }

        public void RunForCompany(Company c)
        {
            var api = new WebClient();
            var authorization = string.Format("{0};{1};{2}", c.PublicID, ConfigurationManager.AppSettings["ApiKey"], ConfigurationManager.AppSettings["ApiSecret"]);
            api.Headers.Add("Authorization", authorization);
            api.Headers.Add("Content-Type", "application/json");
            var json = api.DownloadString(string.Format("{0}{1}/{2}", ConfigurationManager.AppSettings["ApiUri"], "fusion", "configurations"));
            var configs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

            foreach (var config in configs)
            {
                if (config.ContainsKey("TypeName"))
                {
                    if (!string.IsNullOrEmpty(config["TypeName"].ToString()))
                    {
                        string typeName = config["TypeName"].ToString().Trim();
                        Trace.TraceInformation("Fusion Type Name: Locating {0}", typeName);

                        var ext = Items.SingleOrDefault(i => i.Value.GetType().Name.Equals(typeName));
                        if (ext != null)
                        {
                            config.Add("CompanyID", c.PublicID);
                            ext.Value.Synchronize(config);
                        }
                    }
                }
            }
        }
    }

    [DisallowConcurrentExecution()]
    public class FusionSynchronizationJob: BaseJob, IJob, IScheduledJob
    {
        public override bool Enabled { get { return GetEnabledFromConfig(this.GetType().Name); } }
        public override int IntervalInMinutes { get { return GetIntervalFromConfig(this.GetType().Name); } }
        public override string JobName { get { return "Fusion Synchronization"; } }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Trace.TraceInformation(START_JOB_MESSAGE, JobName);

                if (StartJob())
                {
                    #region DI

                    var builder = new ContainerBuilder();

                    var folder = AppDomain.CurrentDomain.BaseDirectory;

                    var files = System.IO.Directory.GetFiles(folder, "d360.extensions.*.dll");

                    var assemblies = (
                                     from f in files
                                     select Assembly.LoadFrom(f)
                                     ).ToArray();
                    builder.RegisterAssemblyTypes(assemblies).As<IFusionSynchronizationSource>();
                    builder.RegisterType<FusionProcessor>().As<FusionProcessor>().AsSelf();

                    var container = builder.Build();

                    var processor = container.Resolve<FusionProcessor>();

                    #endregion

                    bool anyFailed = false;
                    ctx.Companies.ToList().AsParallel().WithDegreeOfParallelism(1).ForAll(c =>
                    {
                        try
                        {
                            bool okToRun = true;
                            var targetCompanyID = (ConfigurationManager.AppSettings["TargetCompanyID"] != null) ? ConfigurationManager.AppSettings["TargetCompanyID"] : string.Empty;
                            if (!string.IsNullOrEmpty(targetCompanyID)) okToRun = targetCompanyID.ToLower() == c.PublicID.ToString().ToLower();
                            if (okToRun)
                            {
                                Trace.TraceInformation(CONNECTING_TO_COMPANY, c.Name, JobName);
                                processor.RunForCompany(c);
                                Trace.TraceInformation(SUCCESS_COMPLETE_MESSAGE, JobName, c.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            anyFailed = true;
                            Trace.TraceError(JOB_COMPANY_ERROR_MESSAGE, JobName, c.Name, ex.Message);
                        }
                    });

                    StopJob(anyFailed ? "ERROR: See event log" : "OK");
                }
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
    }
}
