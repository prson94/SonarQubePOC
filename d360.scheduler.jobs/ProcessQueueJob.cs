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
    public class QueueProcessor
    {
        CompanyContext Context;
        readonly IEnumerable<Meta<IQueueAction>> Items;

        public QueueProcessor(CompanyContext context, IEnumerable<Meta<IQueueAction>> items)
        {
            Context = context;
            Items = items;
        }

        public void RunForCompany()
        {
            List<QueueItem> items = null;

            #region Process all queue items for the current company

            while (Context.QueueItems.Any())
            {
                items = Context.QueueItems.OrderBy(i => i.Date).Take(250).ToList();

                // Mark items as being processed by the current machine so another queue processor does not pick them up.
                items.ForEach(i => { i.MachineAssigned = Environment.MachineName; });
                Context.SaveChanges();

                items.ForEach(item => {
                    if (!string.IsNullOrEmpty(item.Action))
                    {
                        var ext = Items.SingleOrDefault(i => i.Value.GetType().Name.Equals(item.Action));
                        if (ext != null)
                        {
                            var success = false;

                            try
                            {
                                success = ext.Value.ProcessMessage(item);
                            }
                            catch
                            {
                            }

                            if (success)
                            {
                                Context.QueueItems.Remove(item);
                            }
                            else
                            {
                                item.MachineAssigned = string.Empty;
                            }
                        }
                    }                
                });

                Context.SaveChanges();   //Saves any that can be removed from queue.
            }

            #endregion

            #region Destroy Objects

            items = null;
            //Context.Dispose();

            #endregion
        }
    }

    [DisallowConcurrentExecution()]
    public class ProcessQueueJob: BaseJob, IJob, IScheduledJob
    {
        public override bool Enabled { get { return GetEnabledFromConfig(this.GetType().Name); } }
        public override int IntervalInMinutes { get { return GetIntervalFromConfig(this.GetType().Name); } }
        public override string JobName { get { return "Queue Processor"; } }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Trace.TraceInformation(START_JOB_MESSAGE, JobName);

                Community.Companies.ToList().AsParallel().WithDegreeOfParallelism(1).ForAll(c =>
                {
                    try
                    {
                        #region DI

                        var builder = new ContainerBuilder();

                        var companyCtx = new CompanyContext(c.ID, 0, true);
                        builder.RegisterInstance<CompanyContext>(companyCtx);
                            
                        var actions = Assembly.Load("d360.extensions.queue.actions");
                        builder.RegisterAssemblyTypes(actions).As<IQueueAction>();
                        builder.RegisterType<QueueProcessor>().As<QueueProcessor>().AsSelf();
                        var container = builder.Build();

                        var processor = container.Resolve<QueueProcessor>();

                        #endregion

                        processor.RunForCompany();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(JOB_COMPANY_ERROR_MESSAGE, JobName, c.Name, ex.Message);
                    }
                });
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
