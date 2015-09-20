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

namespace d360.scheduler.jobs
{
    public class IndexProcessor
    {
        readonly IEnumerable<Meta<IIndexer>> Indexers;
        ISearchSource SearchSource;

        public IndexProcessor(ISearchSource searchSource, IEnumerable<Meta<IIndexer>> indexers)
        {
            SearchSource = searchSource;
            Indexers = indexers;
        }

        public void RunForCompany(Company c)
        {
            Trace.TraceInformation("Processing {0} indexers.", Indexers.Count());
            SearchSource.ClearIndex(c.ID);
            foreach (var indexer in Indexers)
            {
                indexer.Value.Build();
            }
        }
    }

    [DisallowConcurrentExecution()]
    public class IndexerJob: BaseJob, IJob, IScheduledJob
    {
        IContainer Container;

        public override bool Enabled { get { return GetEnabledFromConfig(this.GetType().Name); } }
        public override int IntervalInMinutes { get { return GetIntervalFromConfig(this.GetType().Name); } }
        public override string JobName { get { return "Indexer"; } }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Trace.TraceInformation(START_JOB_MESSAGE, JobName);

                if (StartJob())
                {
                    #region Get assemblies

                    var folder = AppDomain.CurrentDomain.BaseDirectory;

                    var files = System.IO.Directory.GetFiles(folder, "d360.extensions.*.dll");

                    var assemblies = (
                                     from f in files
                                     select Assembly.LoadFrom(f)
                                     ).ToArray();

                    #endregion

                    bool anyFailed = false;

                    Community.Companies.ToList().AsParallel().WithDegreeOfParallelism(1).ForAll(c =>
                    {
                        try
                        {
                            #region DI for company

                            Trace.TraceInformation("Scanning for indexers.");

                            var builder = new ContainerBuilder();
                            builder.RegisterAssemblyTypes(assemblies).As<ISearchSource>();
                            builder.RegisterAssemblyTypes(assemblies).As<IIndexer>();
                            builder.RegisterType<IndexProcessor>().As<IndexProcessor>().AsSelf();

                            var companyCtx = new CompanyContext(c.ID, 0, true);

                            builder.RegisterInstance<CompanyContext>(companyCtx);

                            Container = builder.Build(); 

                            var processor = Container.Resolve<IndexProcessor>();

                            #endregion

                            processor.RunForCompany(c);
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
