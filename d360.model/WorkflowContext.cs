using d360.core.entities.Workflow;
using System;
using System.Linq;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using d360.core.entities.Contracts;
using System.Collections.Generic;
using d360.extensions;
using System.Reflection;
using Autofac.Features.Metadata;
using Autofac;

namespace d360.model
{
    [DbConfigurationType(typeof(AzureConfiguration))]
    public class WorkflowContext : DbContext
    {
        #region Ctors

        public WorkflowContext(string connectionString, int companyID, int resourceID)
            : base(connectionString)
        {
            CurrentCompanyID = companyID;
            CurrentResourceID = resourceID;

            //output queries in debug mode to console
            if (System.Diagnostics.Debugger.IsAttached)
                this.Database.Log = Console.Write;

            var builder = new ContainerBuilder();

            var folder = AppDomain.CurrentDomain.BaseDirectory;

            var files = System.IO.Directory.GetFiles(folder, "d360.extensions.*.dll");

            var assemblies = (
                             from f in files
                             select Assembly.LoadFrom(f)
                             ).ToArray();
            builder.RegisterAssemblyTypes(assemblies).As<IWorkflowActivity>();
            var container = builder.Build();
            //var processor = container.Resolve<FusionProcessor>();
        }

        #endregion

        #region Properties

        readonly IEnumerable<Meta<IWorkflowActivity>> ActivityTypes;

        public int CurrentResourceID { get; set; }
        public int CurrentCompanyID { get; set; }

        public ObjectContext ObjectContext
        {
            get
            {
                try
                {
                    return ((IObjectContextAdapter)this).ObjectContext;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        #endregion

        #region DbSets

        public DbSet<WorkflowEventRegistration> WorkflowEventRegistrations { get; set; }

        public DbSet<WorkflowType> WorkflowTypes { get; set; }

        public DbSet<WorkflowVersion> WorkflowVersions { get; set; }

        public DbSet<WorkflowVersionStep> WorkflowVersionSteps { get; set; }

        public DbSet<WorkflowVersionStepTransition> WorkflowVersionStepTransitions { get; set; }

        public DbSet<WorkflowItem> WorkflowItems { get; set; }

        public DbSet<WorkflowItemStep> WorkflowItemSteps { get; set; }

        public DbSet<WorkflowItemStepTransition> WorkflowItemStepTransitions { get; set; }

        #endregion

        #region Generic Methods

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.Configuration.AutoDetectChangesEnabled = false;
            base.Configuration.ProxyCreationEnabled = false;
            base.Configuration.LazyLoadingEnabled = false;

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            int returnValue = 0;

            foreach (var entry in ObjectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added))
            {
                #region Business logic : ICreatedMetadata
                if (entry.Entity is ICreatedMetadata)
                {
                    var o = entry.Entity as ICreatedMetadata;
                    o.CreatedBy = CurrentResourceID;
                    o.CreatedOn = DateTime.UtcNow;
                }
                #endregion
            }

            foreach (var entry in ObjectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Unchanged | EntityState.Modified | EntityState.Deleted))
            {
                #region Business logic : IUpdatedMetadata
                if (entry.Entity is IUpdatedMetadata)
                {
                    var o = entry.Entity as IUpdatedMetadata;
                    o.UpdatedBy = CurrentResourceID;
                    o.UpdatedOn = DateTime.UtcNow;
                }
                #endregion
            }

            try
            {
                returnValue = base.SaveChanges();
            }
            catch (OptimisticConcurrencyException)
            {
            }

            return returnValue;
        }

        #endregion

        #region Engine Methods

        public WorkflowItem CreateWorkflowItem(int workflowTypeID)
        {
            var version = WorkflowVersions
                .Include(i => i.Steps)
                .Where(i => i.TypeID == workflowTypeID)
                .OrderByDescending(i => i.CreatedOn)
                .FirstOrDefault();

            var stepIDs = version.Steps.Select(i => i.ID).ToList();

            var transitions = WorkflowVersionStepTransitions
                .Where(i => stepIDs.Contains(i.FromVersionStepID) || stepIDs.Contains(i.ToVersionStepID))
                .ToList();

            var item = new WorkflowItem {
                Active = true,
                StartedBy = 0, StartedOn = DateTime.UtcNow,
                UpdatedBy = 0, UpdatedOn = DateTime.UtcNow,
                VersionID = 1
            };

            WorkflowItems.Add(item);
            SaveChanges();

            //initiate first step.
            var firstVersionStep = version.Steps.Single(s => s.StepType == core.enums.Workflow.StepType.Start);

            var firstItemStep = new WorkflowItemStep { Date = DateTime.UtcNow, Step = firstVersionStep, Fields = "", Settings = "" };
            item.Steps.Add(firstItemStep);

            //var activity = ActivityTypes.SingleOrDefault(i => i.ID == firstItemStep.Step.ActivityType);

            ////execute this activity.

            ////Find the next activities from the start.
            //var nextTransitions = transitions.Where(t => t.FromVersionStepID == firstVersionStep.ID).ToList();

            ////Execute ALWAYS transitions.
            //nextTransitions.Where(i => i.LinkType == core.enums.Workflow.LinkType.Always);

            /*
             NOTES: 
             May need to include a TEST flag on the workflow.Item table so we can mark something as a test and not actually 
             process the changes. The flag would be set when a user wants to test the development of workflow.

             May need to extract a few statements from above code and make it more generic.
             */

            return item;
        }

        #endregion
    }
}
