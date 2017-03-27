using d360.core.entities.Workflow;
using d360.core.enums.Workflow;
using d360.core.workflow;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {

        #region DbSets

        public DbSet<WorkflowEventRegistration> WorkflowEventRegistrations { get; set; }

        public DbSet<core.entities.Workflow.Type> WorkflowTypes { get; set; }

        public DbSet<WorkflowVersion> WorkflowVersions { get; set; }

        public DbSet<WorkflowVersionStep> WorkflowVersionSteps { get; set; }

        public DbSet<WorkflowVersionStepTransition> WorkflowVersionStepTransitions { get; set; }

        public DbSet<WorkflowItem> WorkflowItems { get; set; }

        public DbSet<WorkflowItemStep> WorkflowItemSteps { get; set; }

        public DbSet<WorkflowItemStepTransition> WorkflowItemStepTransitions { get; set; }

        #endregion


        #region Engine Methods

        public async Task<WorkflowItem> CreateWorkflowItem(int workflowTypeID, string @object, int objectID, bool isTest = false)
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

            var item = new WorkflowItem
            {
                Object = @object,
                ObjectID = objectID,
                Active = true,
                StartedBy = 0,
                StartedOn = DateTime.UtcNow,
                UpdatedBy = 0,
                UpdatedOn = DateTime.UtcNow,
                VersionID = version.ID,
                IsTest = isTest
            };

            WorkflowItems.Add(item);
            SaveChanges();

            //initiate first step.
            var firstVersionStep = version.Steps.Single(s => s.StepType == StepType.Start);

            var firstItemStep = new WorkflowItemStep { StartedOn = DateTime.UtcNow, StartedBy = CurrentResourceID, Step = firstVersionStep, Fields = "<fields/>", Settings = "<settings/>", ItemID = item.ID };
            WorkflowItemSteps.Add(firstItemStep);
            SaveChanges();

            await ExecuteStep(firstItemStep.ID, workflowTypeID);

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

        public async Task ExecuteStep(long itemStepID, int workflowId)
        {
            var itemStep = getWorkflowItemStep(itemStepID);
            
            switch (itemStep.Step.ActivityType)
            {
                case WorkflowActivityType.EmailNotification:
                    await SendWorkflowEmail(itemStep.Step, workflowId);
                    break;
                case WorkflowActivityType.Form:
                    break;
                case WorkflowActivityType.StatusChange:
                    break;
                default:
                    break;
            }
            //var activityType = ActivityTypes.SingleOrDefault(i => i.Value.ID == (int)itemStep.Step.ActivityType);
            //if (activityType == null)
            //    throw new ApplicationException($"Item Step does not correspond to any known activity type of {itemStep.Step.ActivityType}.");

            //activityType.Value.Execute(itemStep.Settings);

            itemStep.CompletedOn = DateTime.UtcNow;
            itemStep.CompletedBy = CurrentResourceID;
            SaveChanges();
        }

        private async Task SendWorkflowEmail(WorkflowVersionStep step, int workflowId)
        {
            // call proc for details who to email
            var users = Query<dynamic>("[utility].[GetOwnersForWorkflowV2] @id", new { id = workflowId });

            if (string.IsNullOrEmpty(step.Settings)) throw new Exception("INVALID EMAIL CONFIGURATION FOR SPECIFIED STEP.");

            // build email from step settings.
            var emailSettings = WorkflowEmailModel.ParseFromXml(XElement.Parse(step.Settings));

            //email the users
            /*foreach (var user in users)
            {
                await extensions.mail.SimpleMessage.SendMessage(emailSettings.SubjectTemplate, (string)user.Email, (string)user.FirstName + " " + (string)user.LastName, emailSettings.BodyTemplate);
            }*/
        }

        public void DetermineTransitionBasedOnPreviousStepConditions(long itemStepID)
        {
            var itemStep = getWorkflowItemStep(itemStepID, true);
            var possibleTransitions = GetTransitionsForCompletedStep(itemStep);

            //itemStep.SettingsDocument.
        }

        /// <summary>
        /// Gets a list of possible transitions based on a completed workflow item step.
        /// </summary>
        /// <param name="itemStepID">The workflow item step ID.
        /// <returns>A list of possible transitions.</returns>
        public List<WorkflowVersionStepTransition> GetTransitionsForCompletedStep(long itemStepID)
        {
            var itemStep = getWorkflowItemStep(itemStepID, true);
            return GetTransitionsForCompletedStep(itemStep);
        }

        /// <summary>
        /// Gets a list of possible transitions based on a completed workflow item step.
        /// </summary>
        /// <param name="itemStep">The workflow item step model.</param>
        /// <returns>A list of possible transitions.</returns>
        public List<WorkflowVersionStepTransition> GetTransitionsForCompletedStep(WorkflowItemStep itemStep)
        {
            return WorkflowVersionStepTransitions
                .Include(i => i.FromVersionStep)
                .Include(i => i.ToVersionStep)
                .Where(i => i.FromVersionStepID == itemStep.StepID)
                .ToList();
        }

        /// <summary>
        /// Gets the active workflow item step based on a given ID.
        /// </summary>
        /// <param name="itemStepID">The item ID</param>
        /// <returns>An active workflow item step model.</returns>
        private WorkflowItemStep getWorkflowItemStep(long itemStepID, bool isStepCompleted = false)
        {
            var itemStep = WorkflowItemSteps.Include(i => i.Step).SingleOrDefault(i => i.ID == itemStepID);
            if (itemStep == null)
                throw new ApplicationException("Item Step ID does not correspond to a valid workflow item step.");
            if (!isStepCompleted && itemStep.CompletedOn.HasValue)
                throw new ApplicationException("Item Step has already been completed.");

            return itemStep;
        }

        #endregion
    }
}
