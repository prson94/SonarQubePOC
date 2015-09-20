using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Autofac;
using Autofac.Features.Metadata;
using d360.extensions;
using Quartz;

namespace d360.scheduler.jobs.remote
{
    public class FusionProcessor
    {
        readonly IEnumerable<Meta<IFusionSynchronizationSource>> Items;

        public FusionProcessor(IEnumerable<Meta<IFusionSynchronizationSource>> items)
        {
            Items = items;
        }

        public void Run()
        {
            var api = new WebClient();
            var companyID = ConfigurationManager.AppSettings["TargetCompanyID"];
            var authorization = string.Format("{0};{1};{2}", companyID, ConfigurationManager.AppSettings["ApiKey"], ConfigurationManager.AppSettings["ApiSecret"]);
            api.Headers.Add("Authorization", authorization);
            api.Headers.Add("Content-Type", "application/json");
            var json = api.DownloadString(string.Format("{0}fusion/configurations", ConfigurationManager.AppSettings["ApiUri"]));
            var configs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

            foreach (var config in configs)
            {
                if (config.ContainsKey("TypeName"))
                {
                    if (!string.IsNullOrEmpty(config["TypeName"].ToString()))
                    {
                        var ext = Items.SingleOrDefault(i => i.Value.GetType().Name.Equals(config["TypeName"].ToString().Trim()));
                        if (ext != null)
                        {
                            config.Add("CompanyID", companyID);
                            ext.Value.Synchronize(config);
                        }
                    }
                }
            }
        }
    }

    [DisallowConcurrentExecution()]
    public class FusionSynchronizationJob: IJob, IScheduledJob
    {
        public bool Enabled { get { return true; } }
        public int IntervalInMinutes { get { return 360; } }
        public string JobName { get { return "Fusion Synchronization"; } }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
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

                processor.Run();
            }
            catch (Exception ex)
            {
                throw new JobExecutionException("Exception occured when trying to run the FusionSynchronizationJob", ex, false);
            }
            finally
            {
            }
        }
    }
}
