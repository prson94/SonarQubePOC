using d360.core.entities.Workflow;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.model.workflow;
using Dapper;
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

        public WorkflowItem CreateWorkflowItem(int workflowTypeID, string @object, int objectID, string criteria, bool isTest = false)
        {
            if(!WorkflowRegistrationCriteriaProcessor.Evaluate(this, @object, objectID, criteria))
            {
                System.Diagnostics.Debug.WriteLine("CURRENT ITEM DOESNT MATCH CRITERIA FOR THE WORKFLOW");

                return null;
            }

            //check if the current item meets the criteria if any for this workflow.

            var version = WorkflowVersions
                .Include(i => i.Steps)
                .Where(i => i.TypeID == workflowTypeID)
                .OrderByDescending(i => i.Version)
                .FirstOrDefault();

            var stepIDs = version.Steps.Select(i => i.ID).ToList();

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

            //initiate start step and mark as completed
            var firstVersionStep = version.Steps.Single(s => s.StepType == StepType.Start);

            var firstItemStep = new WorkflowItemStep { CompletedBy = CurrentResourceID, CompletedOn = DateTime.UtcNow, StartedOn = DateTime.UtcNow, StartedBy = CurrentResourceID, Step = firstVersionStep, Fields = "<fields/>", Settings = "<settings/>", ItemID = item.ID };
            
            WorkflowItemSteps.Add(firstItemStep);
            SaveChanges();

            var transitions = WorkflowVersionStepTransitions
                .Where(i => i.FromVersionStepID == firstVersionStep.ID)
                .ToList();

            StartTransitions(transitions, item.ID);
            

            return item;
        }

        private void StartTransitions(List<WorkflowVersionStepTransition> transitions, long itemID)
        {
            var events = new List<EventInfo>();

            foreach (var transition in transitions)
            {
                events.Add(new EventInfo
                {
                    CompanyID = CurrentCompanyID,
                    DomainPrefix = CurrentCompanyDomain,
                    ResourceID = CurrentResourceID,                                        
                    WorkflowItemID = itemID,                    
                    VersionStepTransitionID = transition.ID,
                    Action = ChangeType.Add // irrelevant
                });
            }
            
            //add topic messages for the transitions
            QueueSource.CreateTopicMessages(events);
        }

        /// <summary>
        /// Evaulate a given workflow transition,  if we succeed we need to add new events for the 
        /// step we are transitioning to with the step id
        /// </summary>
        /// <param name="versionStepTransitionID"></param>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public async Task EvaluateWorkflowTransition(long versionStepTransitionID, long itemID, string @object, int objectID)
        {
            var transition = WorkflowVersionStepTransitions
                .Where(i => i.ID == versionStepTransitionID).FirstOrDefault();

            if (transition == null) throw new Exception("ERROR - UNABLE TO LOCATE THE SPECIFIED WORKFLOW TRANSITION STEP");

            bool transitionPassed = false;
            // check the transition type.  Always always goes to next step, condition we need
            // to evaulate the condition to determine if we go to next step
            // timer we dont worry about here some job will keep track of that
            switch (transition.TransitionType)
            {
                case TransitionType.Always:
                    transitionPassed = true;
                    break;
                case TransitionType.Condition:
                    //evaluate the condition then determine if we move to next step
                    transitionPassed = WorkflowRegistrationCriteriaProcessor.Evaluate(this, @object, objectID, transition.Condition);                    
                    break;                                
            }

            if (transitionPassed)
            {
                var fromItemStep = WorkflowItemSteps.Where(i => i.ItemID == itemID && i.StepID == transition.FromVersionStepID).FirstOrDefault();

                if (fromItemStep == null) throw new Exception("ERROR - CANNOT FIND ITEM FROM STEP");

                // insert item step record for the to item step

                var toItemStep = new WorkflowItemStep { StartedOn = DateTime.UtcNow, StartedBy = CurrentResourceID,
                    Step = transition.ToVersionStep,
                    Fields = "<fields/>", Settings = "<settings/>",
                    ItemID = itemID };

                WorkflowItemSteps.Add(toItemStep);
                SaveChanges();

                // insert record into itemsteptransition
                WorkflowItemStepTransition trans = new WorkflowItemStepTransition
                {
                    Condition = "<condition></condition>",
                    Date = DateTime.UtcNow,
                    FromItemStepID = fromItemStep.ID,
                    ToItemStepID = toItemStep.ID
                };

                WorkflowItemStepTransitions.Add(trans);
                SaveChanges();

                // add event queue item for the step
                var events = new List<EventInfo>();

                events.Add(new EventInfo
                {
                    CompanyID = CurrentCompanyID,
                    DomainPrefix = CurrentCompanyDomain,
                    ResourceID = CurrentResourceID,
                    WorkflowItemID = itemID,
                    ItemStepID = toItemStep.ID,
                    Action = ChangeType.Add // irrelevant
                });
                
                //add topic messages for the transitions
                QueueSource.CreateTopicMessages(events);
            }

        }

        /// <summary>
        /// Evaluate a given workflow step, if we succeed we need to add a new event for the transitions that follow
        /// if a complete step we just mark it as done.
        /// </summary>
        /// <param name="itemStepID"></param>
        /// <param name="itemID"></param>
        /// <returns></returns>
        public async Task ExecuteStep(long itemStepID, long itemID)
        {
            bool isStepCompleted = false;
            var itemStep = getWorkflowItemStep(itemStepID);

            var stepType = itemStep.Step.StepType;
            
            if (stepType == StepType.Task)
            {
                switch (itemStep.Step.ActivityType)
                {
                    case WorkflowActivityType.EmailNotification:
                        await SendWorkflowEmail(itemStep.Step);
                        isStepCompleted = true;
                        break;
                    case WorkflowActivityType.Form:
                        // send form notification to owners
                        await SendFormWorkflowEmail(itemStep.Step, itemStepID, itemID);
                        break;
                    case WorkflowActivityType.StatusChange:                        
                        // change the status of this item
                        isStepCompleted = true;
                        break;
                    default:
                        isStepCompleted = true;
                        break;
                }
            }
            else if(stepType == StepType.Finish || stepType == StepType.Terminate)
            {
                // if the task is a finish or terminate task we need to mark the workflow instance as completed and the task as completed
                isStepCompleted = true;

                var item = WorkflowItems.Where(x => x.ID == itemID).FirstOrDefault();

                if (item == null) throw new Exception("ERROR - CANNOT FIND THE WORKFLOW INSTANCE THAT WE NEED TO MARK AS COMPLETED");

                item.CompletedBy = CurrentResourceID;
                item.CompletedOn = DateTime.UtcNow;
                SaveChanges();
            }

            if (isStepCompleted)
            {
                MarkStepAsCompleteAndContinue(itemStep, itemID);
            }

        }

        public void MarkStepAsCompleteAndContinue(WorkflowItemStep itemStep, long itemID)
        {
            // mark step as completed
            itemStep.CompletedOn = DateTime.UtcNow;
            itemStep.CompletedBy = CurrentResourceID;
            SaveChanges();

            // get the transitions for this step and add events
            var transitions = WorkflowVersionStepTransitions
            .Where(i => i.FromVersionStepID == itemStep.StepID)
            .ToList();

            StartTransitions(transitions, itemID);
        }

        private async Task SendFormWorkflowEmail(WorkflowVersionStep step, long itemStepID, long itemId)
        {
            //send an email to the owners with a form link
            var users = Query<dynamic>("[utility].[GetOwnersForWorkflowV2] @id", new { id = step.Version.TypeID });

            var url = "";
            var prefix = "";
            using (var cnn = new System.Data.SqlClient.SqlConnection(core.constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();

                prefix = cnn.Query<string>(@"select UrlPrefix from CompanyDomainSetting where CompanyID = @c and IsPrimary = 1", new { c = CurrentCompanyID }).FirstOrDefault();

                cnn.Close();                
            }

            url += $"https://{prefix}.data3sixty.com/workflow/form/{step.Version.TypeID}/{itemStepID}/{itemId}";
            

            var emailSubject = $"Data3Sixty - Workflow [{step.Version.Type.Name}] - Form";
            var emailBody = $"The Data3Sixty workflow [{step.Version.Type.Name}] has generated a form that you need to complete.  Please complete the form at {url}";

            foreach (var user in users)
            {
                await extensions.mail.SimpleMessage.SendMessage(emailSubject, (string)user.Email, (string)user.FirstName + " " + (string)user.LastName, emailBody);
            }

        }

        private async Task SendWorkflowEmail(WorkflowVersionStep step)
        {
            
            // call proc for details who to email
            var users = Query<dynamic>("[utility].[GetOwnersForWorkflowV2] @id", new { id = step.Version.TypeID });

            if (string.IsNullOrEmpty(step.Settings)) throw new Exception("INVALID EMAIL CONFIGURATION FOR SPECIFIED STEP.");

            // build email from step settings.
            var emailSettings = WorkflowEmailModel.ParseFromXml(XElement.Parse(step.Settings));

            //email the users
            foreach (var user in users)
            {
                await extensions.mail.SimpleMessage.SendMessage(emailSettings.SubjectTemplate, (string)user.Email, (string)user.FirstName + " " + (string)user.LastName, emailSettings.BodyTemplate);
            }
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
