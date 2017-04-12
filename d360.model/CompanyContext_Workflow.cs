using d360.core.entities.Workflow;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.model.workflow;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
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

        public DbSet<WorkflowItemAssignment> WorkflowItemAssignments { get; set; }

        public DbSet<WorkflowItemStep> WorkflowItemSteps { get; set; }

        public DbSet<WorkflowItemStepTransition> WorkflowItemStepTransitions { get; set; }

        #endregion


        #region Engine Methods

        private bool DoesWorkflowApply(EventObjectInfo objectInfo, WorkflowEventRegistration registration)
        {
            var workflowName = "";
            if (registration.Type != null)
                workflowName = registration.Type.Name;

            Console.WriteLine($"DEBUG - TESTING TO SEE IF ${objectInfo.Object} - {objectInfo.ObjectID} IS VALID FOR WORKFLOW {workflowName}");

            if (!WorkflowRegistrationCriteriaProcessor.Evaluate(this, objectInfo.Object.ToString(), objectInfo.ObjectID, registration.Condition))
            {
                Console.WriteLine("DEBUG - CURRENT ITEM DOESNT MATCH CRITERIA FOR THE WORKFLOW");

                return false;
            }

            //if the type is artifacttype check if a specific taxonomy type id was specified in the registration settings.
            if(!string.IsNullOrEmpty(registration.Settings))
            {
                var settingsModel = WorkflowRegistrationSettingsModel.parseXml(XElement.Parse(registration.Settings));

                if(settingsModel.TaxonomyTypeID > 0)
                {
                    Console.WriteLine("DEBUG - CURRENT WORKFLOW IS SPECIFIC TO A PARTICULAR TAXONOMY TYPE. CHECKING INPUT OBJECT AGAINST TAXONOMY TYPE ID");

                    var artifact = Artifacts.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();

                    if(artifact != null && artifact.TaxonomyTypeID != settingsModel.TaxonomyTypeID)
                    {
                        Console.WriteLine($"DEBUG - OBJECT TAXONOMY TYPE ID {artifact.TaxonomyTypeID} DOES NOT MATCH {settingsModel.TaxonomyTypeID}");

                        return false;
                    }
                }
            }
            
            Console.WriteLine("DEBUG - OBJECT MATCHES SPECIFIED CRITERIA");

            return true;
        }

        public bool ExecuteScheduledWorkflow(WorkflowEventRegistration registration)
        {
            Console.WriteLine($"DEBUG - CHECKING IF SCHEDULED WORKFLOW SHOULD RUN TYPE ID {registration.TypeID}");

            //check the last run date of this workflow against how often it runs
            if(registration.ChangeType != ChangeType.Schedule)
            {
                Console.WriteLine($"DEBUG - CURRENT REGISTRATION IS NOT OF CHANGE TYPE SCHEDULE NOT RUNNING.");

                return false;
            }

            if(string.IsNullOrEmpty(registration.Settings))
            {
                Console.WriteLine("DEBUG - CURRENT WORKFLOW DOESNT HAVE ANY SETTINGS CANNOT CONTINUE.");

                return false;
            }

            var settingsXml = XElement.Parse(registration.Settings);

            var scheduledDays = 1;
            if (settingsXml.Element("ScheduleInterval") != null)
            {
                int.TryParse(settingsXml.Element("ScheduleInterval").Value, out scheduledDays);
            }

            if(!registration.LastExecuted.HasValue || (registration.LastExecuted.HasValue && registration.LastExecuted.GetValueOrDefault().AddDays(scheduledDays) <= DateTime.UtcNow ))
            {
                //evaluate objects that are part of this workflow
                switch ((registration.Object ?? "").ToUpper())
                {
                    case "ARTIFACTTYPE":
                        var artifacts = Artifacts.Where(x => x.ArtifactTypeID == registration.ObjectID);
                        foreach (var artifact in artifacts)
                        {
                            CreateWorkflowItem(registration.TypeID,
                                    new EventObjectInfo
                                    {
                                        Object = core.SystemObjects.Artifact,
                                        ObjectID = artifact.ID,
                                        ObjectType = core.SystemObjects.ArtifactType,
                                        ObjectTypeID = registration.ObjectID
                                    },
                                    registration,
                                    0);
                        }
                        break;
                    case "TAXONOMYTYPE":
                        var taxonomies = Taxonomies.Where(x => x.TaxonomyTypeID == registration.ObjectID);
                        foreach (var taxonomy in taxonomies)
                        {
                            CreateWorkflowItem(registration.TypeID,
                                    new EventObjectInfo
                                    {
                                        Object = core.SystemObjects.Taxonomy,
                                        ObjectID = taxonomy.ID,
                                        ObjectType = core.SystemObjects.TaxonomyType,
                                        ObjectTypeID = registration.ObjectID
                                    },
                                    registration,
                                    0);
                        }
                        break;
                    default:
                        break;
                }

                //add item record for start and subsequent queue records

                registration.LastExecuted = DateTime.UtcNow;
                Entry(registration).State = EntityState.Modified;
                SaveChanges();
            }

            return false;
        }


        public bool CreateWorkflowItem(int workflowTypeID, EventObjectInfo objectInfo, WorkflowEventRegistration registration, int requestorId, bool isTest = false)
        {            
            //check if the current item meets the criteria if any for this workflow.
            if (!DoesWorkflowApply(objectInfo, registration))
            {
                return false;
            }
            
            registration.LastExecuted = DateTime.UtcNow;
            Entry(registration).State = EntityState.Modified;

            Console.WriteLine($"DEBUG - CREATING NEW WORKFLOW ITEM FOR {objectInfo.Object} - {objectInfo.ObjectID}");

            var version = WorkflowVersions
                .Include(i => i.Steps)
                .Where(i => i.TypeID == workflowTypeID)
                .OrderByDescending(i => i.Version)
                .FirstOrDefault();

            var item = new WorkflowItem
            {
                Object = objectInfo.Object.ToString(),
                ObjectID = objectInfo.ObjectID,
                Active = true,
                StartedBy = requestorId,
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
            if (!string.IsNullOrEmpty(registration.Settings))
            {
                Console.WriteLine("DEBUG - WORKFLOW HAS SETTINGS, STARTING TO SET THOSE.");

                //take the workflow settings right now this is only the visible column and apply these values if present.
                ProcessStartStepSettings(registration.Settings, objectInfo);
            }

            Console.WriteLine("DEBUG - STARTING WORKFLOW TRANSITIONS.");

            StartTransitions(transitions, item.ID, objectInfo);

            Console.WriteLine("DEBUG - WORKFLOW INSTANCE SUCESSFULLY CREATED.");

            return true;
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

                // insert item step record for the to item step if none exist

                if (WorkflowItemSteps.Where(x => x.ItemID == itemID && x.StepID == transition.ToVersionStepID).Any())
                {
                    Console.WriteLine("ERROR ENCOUNTERED CASE WHERE ITEMSTEP DATA ALREADY EXISTS");

                    return;
                }
               

                Console.WriteLine($"DEBUG ADDING WORKFLOW WORKFLOW.ITEMSTEP STEP ID [{transition.ToVersionStepID}] ITEM ID [{itemID}] ");

                var toItemStep = new WorkflowItemStep
                {
                    StartedOn = DateTime.UtcNow,
                    StartedBy = CurrentResourceID,
                    StepID = transition.ToVersionStepID,
                    Fields = "<fields/>",
                    Settings = "<settings/>",
                    ItemID = itemID
                };

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
                        await SendWorkflowEmail(itemStep, objectInfo);
                        isStepCompleted = true;
                        break;
                    case WorkflowActivityType.Form:
                        // send form notification to owners
                        await SendFormWorkflowEmail(itemStep, itemStepID, itemID, objectInfo);
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

                //Mark any assignments as inactive / update them
                CompleteItemAssignments(itemID);
            }
                        
            if (isStepCompleted)
            {
                MarkStepAsCompleteAndContinue(itemStep, itemID, objectInfo);
            }
        }

        private void SaveItemAssignments(IEnumerable<dynamic> users, long itemId)
        {
            foreach (var user in users)
            {
                var assignment = new WorkflowItemAssignment
                {
                    CreatedBy = 0,
                    CreatedOn = DateTime.UtcNow,
                    ItemID = itemId,
                    ResourceObject = "Resource",
                    ResourceObjectID = user.ID,
                    UpdatedBy = 0,
                    UpdatedOn = DateTime.UtcNow
                };

                WorkflowItemAssignments.Add(assignment);
            }
            SaveChanges();
        }

        private void CompleteItemAssignments(long itemID)
        {
            var itemAssignments = WorkflowItemAssignments.Where(x => x.ItemID == itemID);

            foreach (var assignment in itemAssignments)
            {
                WorkflowItemAssignments.Remove(assignment);
            }            

            SaveChanges();
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
                    if (artifact == null) return;
                    artifact.Visible = visiblity;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Intersect:
                    var intersect = Intersects.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (intersect == null) return;
                    intersect.Visible = visiblity;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Taxonomy:
                    var taxonomy = Taxonomies.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (taxonomy == null) return;
                    taxonomy.Visible = visiblity;
                    SaveChanges();
                    break;                                
                case core.SystemObjects.Policy:
                    var policy = Policies.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (policy == null) return;
                    policy.Visible = visiblity;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Rule:
                    var rule = Rules.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (rule == null) return;
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

            if(transitions.Count > 0)
                StartTransitions(transitions, itemID, objectInfo);
        }

        private async Task SendFormWorkflowEmail(WorkflowItemStep item, long itemStepID, long itemId, EventObjectInfo objectInfo)
        {
            //send an email to the owners with a form link
            var users = Query<dynamic>("[utility].[GetOwnersForWorkflowV2] @id, @stepId", new { id = item.Step.Version.TypeID, @stepId = item.Step.ID });
                        
            var url = "";
            var prefix = "";
            using (var cnn = new System.Data.SqlClient.SqlConnection(core.constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();

                prefix = cnn.Query<string>(@"select UrlPrefix from CompanyDomainSetting where CompanyID = @c and IsPrimary = 1", new { c = CurrentCompanyID }).FirstOrDefault();

                cnn.Close();                
            }

            url += $"https://{prefix}.data3sixty.com/workflow/form/{item.Step.Version.TypeID}/{itemStepID}/{itemId}";

            var initiatedBy = "(unknown)";

            if(item.StartedBy > 0)
            {
                var res = GlobalReportingResources.Where(x => x.ResourceID == item.StartedBy).FirstOrDefault();

                if(res != null)
                {
                    initiatedBy = res.FullName;
                }
            }


            //update the xml for the number of users sent the form
            var xml = XElement.Parse(item.Fields);
            xml.Add(new XAttribute("TotalResources", users.Count()));
            item.Fields = xml.ToString();
            SaveChanges();

            var obj = GetObjectDetail(objectInfo.Object, objectInfo.ObjectID);

            var itemName = (obj == null) ? "(unknown)" : obj.Name;
            var emailSubject = $"Data3Sixty - Workflow [{item.Step.Version.Type.Name}] - Form";
            var emailBody = $"<p>The Data3Sixty workflow <b>{item.Step.Version.Type.Name}</b> has generated a form that you need to complete for the item <b>{itemName}</b>.  This workflow was initiated by {initiatedBy}.  Please complete the form at {url}</p>";

            var emailBase = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\"><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"width: 100%; background-color: #54a4da\"><tbody><tr><td><span style=\"float: none; display: inline-block; text-align: left;\"><img alt=\"Data3Sixty, Inc.\" height=\"50\" src=\"https://d3spublic.blob.core.windows.net/images/Logo246x50.jpg\" width=\"246\"></span></td></tr></tbody></table>{emailBody}</body></html>";

            foreach (var user in users)
            {
                await extensions.mail.SimpleMessage.SendMessage(emailSubject, (string)user.Email, (string)user.FirstName + " " + (string)user.LastName, emailBase, true);
            }

            SaveItemAssignments(users, itemId);

        }

        private async Task SendWorkflowEmail(WorkflowItemStep item, EventObjectInfo objectInfo)
        {            

            if (string.IsNullOrEmpty(item.Step.Settings)) throw new Exception("INVALID EMAIL CONFIGURATION FOR SPECIFIED STEP.");

            // build email from step settings.
            var emailSettings = WorkflowEmailModel.ParseFromXml(XElement.Parse(item.Step.Settings));
            

            var url = "";
            var prefix = "";
            using (var cnn = new System.Data.SqlClient.SqlConnection(core.constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();

                prefix = cnn.Query<string>(@"select UrlPrefix from CompanyDomainSetting where CompanyID = @c and IsPrimary = 1", new { c = CurrentCompanyID }).FirstOrDefault();

                cnn.Close();
            }

            url += $"https://{prefix}.data3sixty.com/workflow/details/{item.ItemID}";

            emailSettings.BodyTemplate = ProcessMessageBody(emailSettings.BodyTemplate, objectInfo, prefix, item);

            emailSettings.BodyTemplate = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\"><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"width: 100%; background-color: #54a4da\"><tbody><tr><td><span style=\"float: none; display: inline-block; text-align: left;\"><img alt=\"Data3Sixty, Inc.\" height=\"50\" src=\"https://d3spublic.blob.core.windows.net/images/Logo246x50.jpg\" width=\"246\"></span></td></tr></tbody></table>{emailSettings.BodyTemplate}<p>Item Workflow Details {url}</p></body></html>";
            

            if (emailSettings.RecipientType == EmailTaskRecipientType.Initiator)
            {
                if (item.Item.StartedBy <= 0)
                {
                    Console.WriteLine("ERROR CANNOT DETERMINE WHO TO EMAIL WORKLFOW EMAIL TASK MESSAGE TO.");

                    return;
                }

                var res = GlobalReportingResources.Where(x => x.ResourceID == item.Item.StartedBy).FirstOrDefault();

                if (res == null)
                {
                    Console.WriteLine("ERROR CANNOT FIND THE RESOURCE WHO STARTED THE WORKFLOW TO EMAIL.");

                    return;
                }

                await extensions.mail.SimpleMessage.SendMessage(emailSettings.SubjectTemplate, (string)res.Email, (string)res.FirstName + " " + (string)res.LastName, emailSettings.BodyTemplate, true);
            }
            else if(emailSettings.RecipientType == EmailTaskRecipientType.Owner)
            {
                var users = Query<dynamic>("[utility].[GetOwnersForWorkflowV2] @id, @stepId", new { id = item.Step.Version.TypeID, @stepId = item.Step.ID });

                foreach (var user in users)
                {
                    await extensions.mail.SimpleMessage.SendMessage(emailSettings.SubjectTemplate, (string)user.Email, (string)user.FirstName + " " + (string)user.LastName, emailSettings.BodyTemplate, true);
                }
            }
            else if(emailSettings.RecipientType == EmailTaskRecipientType.SpecificUser)
            {
                if(string.IsNullOrEmpty(emailSettings.SpecificUser))
                {
                    Console.Write("ERROR - NO USER SPECIFIED FOR THE SPECIFIC USER EMAIL TASK.");

                    return;
                }

                await extensions.mail.SimpleMessage.SendMessage(emailSettings.SubjectTemplate, emailSettings.SpecificUser, "", emailSettings.BodyTemplate, true);
            }
        }

        private string ProcessMessageBody(string bodyTemplate, EventObjectInfo objectInfo, string prefix, WorkflowItemStep itemStep)
        {
            var result = bodyTemplate;
            //replace [OBJECT_NAME] with the object name            
            if (result.Contains("[OBJECT_NAME]"))
            {
                //get the objects name
                var item = GetObjectDetail(objectInfo.Object, objectInfo.ObjectID);
                var itemLink = "(unknown item)";

                if (item != null)                
                    itemLink = $"<b><a href=\"https://{prefix}.data3sixty.com/{item.Url}\">{item.Name}</a></b>";

                result = result.Replace("[OBJECT_NAME]", itemLink);
            }

            if (result.Contains("[WORKFLOW_INITIATOR]"))
            {
                var initiator = "unknown user";

                if (itemStep.Item != null && itemStep.Item.StartedBy > 0)
                {
                    var user = GlobalReportingResources.Where(x => x.ResourceID == itemStep.Item.StartedBy).FirstOrDefault();

                    if(user != null)
                    {
                        initiator = user.FullName;
                    }
                }

                result = result.Replace("[WORKFLOW_INITIATOR]", initiator);
            }

            if (Regex.IsMatch(result, "\\[FIELD([0-9.]+)\\]"))
            {
                var fields = Regex.Matches(result, "\\[FIELD([0-9.]+)\\]");

                foreach (var field in fields)
                {
                    var item = field.ToString();

                    var fieldId = 0;

                    var fieldIdStringitem = item.Replace("[FIELD", "");
                    fieldIdStringitem = fieldIdStringitem.Replace("]", "");

                    int.TryParse(fieldIdStringitem, out fieldId);

                    var fieldValue = "";

                    if (fieldId > 0)
                    {
                        var fieldRecord = Fields.Where(x => x.ObjectID == objectInfo.ObjectID && x.ObjectType == objectInfo.Object.ToString() && x.FieldTypeID == fieldId).FirstOrDefault();

                        fieldValue = fieldRecord.FormattedValue;
                    }

                    result = result.Replace(item, fieldValue);
                }
            }

            return result;
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
