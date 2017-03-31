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

        public WorkflowItem CreateWorkflowItem(int workflowTypeID, EventObjectInfo objectInfo, WorkflowEventRegistration registration, bool isTest = false)
        {
            Console.WriteLine($"DEBUG - CREATING NEW WORKFLOW ITEM FOR ${objectInfo.Object} - {objectInfo.ObjectID}");

            if (!WorkflowRegistrationCriteriaProcessor.Evaluate(this, objectInfo.Object.ToString(), objectInfo.ObjectID, registration.Condition))
            {
                Console.WriteLine("DEBUG - CURRENT ITEM DOESNT MATCH CRITERIA FOR THE WORKFLOW");

                return null;
            }

            Console.WriteLine("DEBUG - OBJECT MATCHES SPECIFIED CRITERIA");

            //check if the current item meets the criteria if any for this workflow.

            var version = WorkflowVersions
                .Include(i => i.Steps)
                .Where(i => i.TypeID == workflowTypeID)
                .OrderByDescending(i => i.Version)
                .FirstOrDefault();

            var stepIDs = version.Steps.Select(i => i.ID).ToList();

            var item = new WorkflowItem
            {
                Object = objectInfo.Object.ToString(),
                ObjectID = objectInfo.ObjectID,
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

            Console.WriteLine("DEBUG - PROCESSING START ITEM STEP.");

            var transitions = WorkflowVersionStepTransitions
                .Where(i => i.FromVersionStepID == firstVersionStep.ID)
                .ToList();

            //take any settings from the event registration and apply them in this start step
            if(!string.IsNullOrEmpty(registration.Settings))
            {
                Console.WriteLine("DEBUG - WORKFLOW HAS SETTINGS, STARTING TO SET THOSE.");

                //take the workflow settings right now this is only the visible column and apply these values if present.
                ProcessStartStepSettings(registration.Settings, objectInfo);
            }

            Console.WriteLine("DEBUG - STARTING WORKFLOW TRANSITIONS.");

            StartTransitions(transitions, item.ID, objectInfo);

            Console.WriteLine("DEBUG - WORKFLOW INSTANCE SUCESSFULLY CREATED.");

            return item;
        }

        private void ProcessStartStepSettings(string settings, EventObjectInfo objectInfo)
        {
            //take the settings and see if we need to do anything
            WorkflowRegistrationSettingsModel settingsModel = WorkflowRegistrationSettingsModel.parseXml(XElement.Parse(settings));

            //if there is a Visibility value update the appropriate item
            if (settingsModel.Visible.HasValue)
            {
                Console.WriteLine($"DEBUG - OBJECT TYPE[{objectInfo.ObjectType}] ID[{objectInfo.ObjectID}] VISIBILITY SET TO {settingsModel.Visible}");

                SetObjectVisibility(objectInfo, settingsModel.Visible.GetValueOrDefault());
            }
        }

        private void StartTransitions(List<WorkflowVersionStepTransition> transitions, long itemID, EventObjectInfo objectInfo)
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
                    Action = ChangeType.Add, // irrelevant
                    Object = objectInfo
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
        public async Task EvaluateWorkflowTransition(long versionStepTransitionID, long itemID, EventObjectInfo objectInfo)
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
                    //get the object for this conditoin
                    var item = WorkflowItems.Where(x => x.ID == itemID).FirstOrDefault();

                    if (item == null) throw new Exception("ERROR UNABLE TO GET THE DETAILS FOR THIS WORKFLOW INSTANCE.");
                    //evaluate the condition then determine if we move to next step
                    transitionPassed = WorkflowRegistrationCriteriaProcessor.Evaluate(this, item.Object, item.ObjectID, transition.Condition, itemID);                    
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
                    Action = ChangeType.Add, // irrelevant
                    Object = objectInfo
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
        public async Task ExecuteStep(long itemStepID, long itemID, EventObjectInfo objectInfo)
        {
            bool isStepCompleted = false;
            var itemStep = getWorkflowItemStep(itemStepID);

            //if the step is already done exit
            if (itemStep.CompletedOn.HasValue)
            {
                Console.WriteLine($"STEP WITH ID {itemStepID} HAS ALREADY COMPLETED NOT RERUNNING");

                return;
            }
                        
            var stepType = itemStep.Step.StepType;

            Console.WriteLine($"Debug - Processing step of type {stepType}");

            if (stepType == StepType.Task)
            {
                Console.WriteLine($"Debug - Processing workflow task of type {itemStep.Step.ActivityType}");
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
                        ChangeItemStatus(itemStep.Step,objectInfo);
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

                //mark the visible flag for the specified object as 1
                if(stepType == StepType.Finish) SetObjectVisibility(objectInfo); // only finish steps should set objects as visible

                var item = WorkflowItems.Where(x => x.ID == itemID).FirstOrDefault();

                if (item == null) throw new Exception("ERROR - CANNOT FIND THE WORKFLOW INSTANCE THAT WE NEED TO MARK AS COMPLETED");

                item.CompletedBy = CurrentResourceID;
                item.CompletedOn = DateTime.UtcNow;
                SaveChanges();
            }
            
            if (isStepCompleted)
            {
                MarkStepAsCompleteAndContinue(itemStep, itemID, objectInfo);
            }
        }

        private void ChangeItemStatus(WorkflowVersionStep step, EventObjectInfo objectInfo)
        {
            var xml = step.Settings;
            if (string.IsNullOrEmpty(xml))
            {
                Console.WriteLine("ERROR THE XML FOR THE STATUS CHANGE STEP IS NULL OR EMPTY.  THIS IS NOT VALID.");

                throw new Exception("ERROR - INVALID CONFIGURATION FOR THE STATUS CHANGE TASK.");
            }

            // change the item status to the value specified
            WorkflowStatusModel statusModel = WorkflowStatusModel.ParseFromXml(XElement.Parse(xml));

            //change the objects status field to the specified value
            switch (objectInfo.Object)
            {
                case core.SystemObjects.Artifact:
                    var artifact = Artifacts.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    artifact.Status = statusModel.Status;
                    SaveChanges();
                    break;
            }

        }

        private void SetObjectVisibility(EventObjectInfo objectInfo, bool visiblity = true)
        {
            Console.WriteLine($"Debug - Setting Object {objectInfo.Object} {objectInfo.ObjectID} as visible {visiblity}");

            switch (objectInfo.Object)
            {
                case core.SystemObjects.Artifact:
                    var artifact = Artifacts.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    artifact.Visible = visiblity;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Intersect:
                    var intersect = Intersects.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    intersect.Visible = visiblity;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Taxonomy:
                    var taxonomy = Taxonomies.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    taxonomy.Visible = visiblity;
                    SaveChanges();
                    break;                                
                case core.SystemObjects.Policy:
                    var policy = Policies.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    policy.Visible = visiblity;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Rule:
                    var rule = Rules.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    rule.Visible = visiblity;
                    SaveChanges();
                    break;                                             
                default:
                    break;
            }
        }

        public void MarkStepAsCompleteAndContinue(WorkflowItemStep itemStep, long itemID, EventObjectInfo objectInfo)
        {
            // mark step as completed
            itemStep.CompletedOn = DateTime.UtcNow;
            itemStep.CompletedBy = CurrentResourceID;
            SaveChanges();

            // get the transitions for this step and add events
            var transitions = WorkflowVersionStepTransitions
            .Where(i => i.FromVersionStepID == itemStep.StepID)
            .ToList();

            StartTransitions(transitions, itemID, objectInfo);
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
