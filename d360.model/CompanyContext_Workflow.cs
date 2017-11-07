using d360.core;
using d360.core.entities;
using d360.core.entities.Workflow;
using d360.core.enums.Workflow;
using d360.core.enums;
using d360.core.queue;
using d360.model.workflow;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
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

        public DbSet<WorkflowTaskProcedure> WorkflowTaskProcedures { get; set; }

        #endregion


        #region Engine Methods

        private bool DoesWorkflowApply(EventObjectInfo objectInfo, WorkflowEventRegistration registration)
        {
            var workflowName = "";
            if (registration.Type != null)
                workflowName = registration.Type.Name;

            Console.WriteLine($"DEBUG - TESTING TO SEE IF ${objectInfo.Object} - {objectInfo.ObjectID} IS VALID FOR WORKFLOW {workflowName}");

            string issueObject = "";
            int issueObjectId = -1;

            if(objectInfo.Object == SystemObjects.Issue)
            {
                Console.WriteLine($"DEBUG - WORKFLOW IS AN ISSUE.  DETERMINING WHAT OBJECT THE ISSUE WITH ID {objectInfo.ObjectID} WAS RAISED ON.");

                var issueDetail = Issues.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();

                if(issueDetail == null)
                {
                    Console.WriteLine("ERROR - ISSUE RAISED BUT DOESNT HAVE CORRESPONDING ISSUE RECORD.");

                    return false;
                }

                issueObject = issueDetail.Object;
                issueObjectId = issueDetail.ObjectID;
            }

            if (!WorkflowRegistrationCriteriaProcessor.Evaluate(this, objectInfo.Object.ToString(), objectInfo.ObjectID, registration.Condition, -1, (objectInfo.Score.HasValue ? objectInfo.Score.Value : -1), objectInfo.ChangedFieldIds, issueObject, issueObjectId))
            {
                Console.WriteLine("DEBUG - CURRENT ITEM DOESNT MATCH CRITERIA FOR THE WORKFLOW");

                return false;
            }

            //if the type is artifacttype check if a specific taxonomy type id was specified in the registration settings.
            if(!string.IsNullOrEmpty(registration.Settings))
            {
                var settingsModel = WorkflowRegistrationSettingsModel.parseXml(XElement.Parse(registration.Settings));
            }
            
            Console.WriteLine("DEBUG - OBJECT MATCHES SPECIFIED CRITERIA");

            return true;
        }
        
        public bool AssignActivityWorkflowToNewObject(WorkflowEventRegistration reg, int itemId, int workflowId, int objectId, string @object)
        {
            var item = WorkflowItems.Where(x => x.ID == itemId).FirstOrDefault();

            if (item == null) throw new Exception("invalid workflow item");

            var orgIssue = Issues.Where(x => x.ID == item.ObjectID).FirstOrDefault();

            if (orgIssue == null) throw new Exception("invalid workflow issue");

            //add new issue record
            var issue = new Issue
            {
                Object = @object,
                ObjectID = objectId,
                ObjectType = "IssueType",
                ObjectTypeID = reg.TypeID,
                CreatedBy = CurrentResourceID,
                CreatedOn = DateTime.UtcNow,
                UpdatedBy = CurrentResourceID,
                UpdatedOn = DateTime.UtcNow,
                IssueTypeID = orgIssue.IssueTypeID,
                CommentID =orgIssue.CommentID,
                Criticality = orgIssue.Criticality
            };

            Issues.Add(issue);

            SaveChanges();

            //add new event
            var events = new List<EventInfo>
            {
                new EventInfo
                {
                    CompanyID = CurrentCompanyID,
                    DomainPrefix = CurrentCompanyDomain,
                    ResourceID = CurrentResourceID,
                    Action = ChangeType.Add,
                    Object = new EventObjectInfo
                    {
                        Object = SystemObjects.Issue,
                        ObjectID = issue.ID,
                        ObjectType = SystemObjects.IssueType,
                        ObjectTypeID = orgIssue.IssueTypeID
                    }
                }
            };

            QueueSource.CreateTopicMessages(events);

            return true;
            
        }
        public bool ExecuteTimerSteps()
        {
            var sql = @"select
                            i_s.id 'FromItemStepID'
	                        ,i_s.stepid 'FromStepID'
	                        ,vst.toversionstepid 'ToStepID'
	                        ,vst.settings 'SettingXml'
	                        ,vst.settings.value('(/settings/TimerInterval)[1]', 'int') 'Days'
	                        ,DATEADD(day, vst.settings.value('(/settings/TimerInterval)[1]', 'int'), i_s.CompletedOn) as ShouldRunOn
                            ,i_s.Itemid as ItemID
                            ,vst.id as VersionStepTransitionID
                            ,i.Object
	                        ,i.ObjectID
                        from
                            workflow.itemstep i_s
                            inner join workflow.item i on (i.id = i_s.itemId and i.completedon is null)
	                        inner join workflow.versionsteptransition vst on(vst.fromversionstepid = i_s.stepid and vst.transitiontype = 3)
                        where
							i_s.CompletedOn is null
								and
                            DATEADD(day, vst.settings.value('(/settings/TimerInterval)[1]', 'int'), i_s.StartedOn) <= getutcdate()-- timers that need to be run
                                and
                            not exists(select * from workflow.itemsteptransition s_ist inner join workflow.itemstep s_isf on (s_isf.id = s_ist.fromitemstepid and s_isf.id = i_s.id) inner join workflow.itemstep s_isto on(s_isto.id = s_ist.toitemstepid and s_isto.itemid = i_s.itemid and s_isto.stepid = vst.toversionstepid))";

            var res = Query<dynamic>(sql);

            var events = new List<EventInfo>();

            foreach (var transition in res)
            {
                SystemObjects objectType = SystemObjects.Artifact;
                if (!Enum.TryParse<SystemObjects>(transition.Object, true, out objectType)) continue;

                events.Add(new EventInfo
                {
                    CompanyID = CurrentCompanyID,
                    DomainPrefix = CurrentCompanyDomain,
                    ResourceID = CurrentResourceID,
                    WorkflowItemID = transition.ItemID,
                    VersionStepTransitionID = transition.VersionStepTransitionID,
                    Action = ChangeType.Add, // irrelevant
                    Object = new EventObjectInfo
                    {
                        Object = objectType,
                        ObjectID = transition.ObjectID,
                        ObjectType = SystemObjects.ArtifactType //doesnt matter needs value to serialize and there is no none in the enum                  
                    }
                });
            }

            //add topic messages for the transitions
            if(events.Count > 0) QueueSource.CreateTopicMessages(events);
            

            return true;
        }

        public async Task<bool> ExecuteScheduledWorkflow(WorkflowEventRegistration registration)
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

            var settings = WorkflowEventRegistrationSettingsModel.Parse(registration.Settings);

            int matchingItems = 0;
            List<string> items = new List<string>();


            if(!registration.LastExecuted.HasValue || (registration.LastExecuted.HasValue && registration.LastExecuted.GetValueOrDefault().AddDays(settings.ScheduleInterval) <= DateTime.UtcNow ))
            {
                //evaluate objects that are part of this workflow
                switch ((registration.Object ?? "").ToUpper())
                {
                    case "ARTIFACTTYPE":
                        var artifacts = Artifacts.Where(x => x.ArtifactTypeID == registration.ObjectID);
                        foreach (var artifact in artifacts)
                        {
                            if (await CreateWorkflowItem(registration.TypeID,
                                    new EventObjectInfo
                                    {
                                        Object = core.SystemObjects.Artifact,
                                        ObjectID = artifact.ID,
                                        ObjectType = core.SystemObjects.ArtifactType,
                                        ObjectTypeID = registration.ObjectID
                                    },
                                    registration,
                                    0)) {
                                matchingItems++;
                                items.Add(artifact.DisplayValue);
                            }
                        }
                        break;
                    case "TAXONOMYTYPE":
                        var taxonomies = Taxonomies.Where(x => x.TaxonomyTypeID == registration.ObjectID);
                        foreach (var taxonomy in taxonomies)
                        {
                            if (await CreateWorkflowItem(registration.TypeID,
                                    new EventObjectInfo
                                    {
                                        Object = core.SystemObjects.Taxonomy,
                                        ObjectID = taxonomy.ID,
                                        ObjectType = core.SystemObjects.TaxonomyType,
                                        ObjectTypeID = registration.ObjectID
                                    },
                                    registration,
                                    0))
                            {
                                matchingItems++;
                                items.Add(taxonomy.DisplayValue);
                            }
                        }
                        break;
                    default:
                        break;
                }

                //add item record for start and subsequent queue records
                registration.LastExecuted = DateTime.UtcNow;
                Entry(registration).State = EntityState.Modified;
                SaveChanges();

                //if the scheduled workflow needs an aggregate email send it
                if (settings.SendAggregateEmail && matchingItems > 0)
                {
                    await SendAggregateWorkflowEmail(settings,items);
                }
            }

            return false;
        }


        public async Task<bool> CreateWorkflowItem(int workflowTypeID, EventObjectInfo objectInfo, WorkflowEventRegistration registration, int requestorId, bool isTest = false)
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
                .Where(i => i.TypeID == workflowTypeID && i.ID == registration.Type.PublishedVersionID)
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
            var firstVersionStep = version.Steps.Where(s => s.StepType == StepType.Start).FirstOrDefault();

            if(firstVersionStep == null)
            {
                Console.WriteLine("ERROR - WORKFLOW HAS NO START STEP.  DONE.");

                return true;
            }

            var firstItemStep = new WorkflowItemStep { CompletedBy = CurrentResourceID, CompletedOn = DateTime.UtcNow, StartedOn = DateTime.UtcNow, StartedBy = CurrentResourceID, Step = firstVersionStep, Fields = "<fields/>", Settings = "<settings/>", ItemID = item.ID };

            WorkflowItemSteps.Add(firstItemStep);
            SaveChanges();

            Console.WriteLine("DEBUG - PROCESSING START ITEM STEP.");

            var transitions = WorkflowVersionStepTransitions
                .Where(i => i.FromVersionStepID == firstVersionStep.ID && i.TransitionType != TransitionType.Timer)
                .ToList();

            //take any settings from the event registration and apply them in this start step
            if (!string.IsNullOrEmpty(registration.Settings))
            {
                Console.WriteLine("DEBUG - WORKFLOW HAS SETTINGS, STARTING TO SET THOSE.");

                //take the workflow settings right now this is only the visible column and apply these values if present.
                ProcessStartStepSettings(registration.Settings, objectInfo);
            }

            Console.WriteLine("DEBUG - STARTING WORKFLOW TRANSITIONS.");

            if(transitions.Count > 0)
                await StartTransitions(transitions, item.ID, objectInfo);

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

        private async Task StartTransitions(List<WorkflowVersionStepTransition> transitions, long itemID, EventObjectInfo objectInfo)
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
            await QueueSource.CreateTopicMessagesAsync(events);
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
                    transitionPassed = WorkflowRegistrationCriteriaProcessor.Evaluate(this, item.Object, item.ObjectID, transition.Condition, itemID, -1, objectInfo.ChangedFieldIds);                    
                    break;
                case TransitionType.Timer:
                    //check if this timer transtion has a condition if so evaluate it
                    Console.WriteLine("DEBUG - EVALUATING TIMER TRANSITION");
                    transitionPassed = true;
                    if (!string.IsNullOrEmpty(transition.Condition))
                    {
                        var root = XElement.Parse(transition.Condition);

                        if(root != null && root.Element("Condition") != null)
                        {
                            var transItem = WorkflowItems.Where(x => x.ID == itemID).FirstOrDefault();

                            transitionPassed = WorkflowRegistrationCriteriaProcessor.Evaluate(this, transItem.Object, transItem.ObjectID, root.Element("Condition").Value, itemID, -1, objectInfo.ChangedFieldIds);
                        }
                    }
                    
                    break;                              
            }

            if (transitionPassed)
            {
                var fromItemStep = WorkflowItemSteps.Where(i => i.ItemID == itemID && i.StepID == transition.FromVersionStepID).FirstOrDefault();

                if (fromItemStep == null) throw new Exception("ERROR - CANNOT FIND ITEM FROM STEP");

                long toItemStepID = 0;
                // insert item step record for the to item step if none exist
                var itemStepTo = WorkflowItemSteps.Where(x => x.ItemID == itemID && x.StepID == transition.ToVersionStepID).FirstOrDefault();

                if (itemStepTo == null)
                {
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

                    toItemStepID = toItemStep.ID;
                }
                else
                {
                    Console.WriteLine($"DEBUG ITEMSTEP DATA ALREADY EXISTS {itemStepTo.ID}");

                    toItemStepID = itemStepTo.ID;
                }
                
                if(toItemStepID <= 0)
                {
                    Console.WriteLine($"ERROR - ITEMSTEP ID IS LESS THAN OR EQUAL TO ZERO MEANING IT DOESNT EXIST AND WE CANT INSERT A NEW ONE.  THIS SHOULD NOT HAPPEN!");

                    return;
                }

                // insert record into itemsteptransition
                WorkflowItemStepTransition trans = new WorkflowItemStepTransition
                {
                    Condition = "<condition></condition>",
                    Date = DateTime.UtcNow,
                    FromItemStepID = fromItemStep.ID,
                    ToItemStepID = toItemStepID
                };

                WorkflowItemStepTransitions.Add(trans);
                SaveChanges();

                
                var startEvent = new EventInfo
                {
                    CompanyID = CurrentCompanyID,
                    DomainPrefix = CurrentCompanyDomain,
                    ResourceID = CurrentResourceID,
                    WorkflowItemID = itemID,
                    ItemStepID = toItemStepID,                    
                    Action = ChangeType.Add, // irrelevant
                    Object = objectInfo
                };
                
                //add topic messages for the transitions
                await QueueSource.CreateTopicMessageAsync(startEvent);
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
                        
            if(itemStep.Step == null)
            {
                Console.WriteLine($"STEP WITH ID {itemStepID} HAS NULL STEP REFERENCE CANNOT CONTINUE");

                return;
            }
            
            var stepSettings = WorkflowItemStepSettingModel.ParseXml(itemStep.Step.Settings);

            //does the step need to wait for all transitions before running?
            if (stepSettings.WaitForAllTransitions)
            {
                //get count of the number of transitions to this step
                var expectedTransitionCount = WorkflowVersionStepTransitions.Where(x => x.ToVersionStepID == itemStep.StepID).Count();

                //get count of the completed transitions to this step
                var completedTransitionsCount = WorkflowItemStepTransitions.Where(x => x.ToItemStepID == itemStepID).Count();

                if(expectedTransitionCount != completedTransitionsCount)
                {
                    Console.WriteLine($"STEP WITH ID {itemStepID} HAS WAIT FOR ALL TRANSITIONS TO COMPLETE ENABLED NOT ALL HAVE COMPLETED expected:{expectedTransitionCount} actual completed:{completedTransitionsCount}");

                    return;
                }
            }


            var stepType = itemStep.Step.StepType;

            Console.WriteLine($"Debug - Processing step of type {stepType}");

            if (stepType == StepType.Task)
            {
                Console.WriteLine($"Debug - Processing workflow task of type {itemStep.Step.ActivityType}");
                switch (itemStep.Step.ActivityType)
                {
                    case WorkflowActivityType.EmailNotification:
                        await SendWorkflowEmail(itemStep, objectInfo, stepSettings);
                        isStepCompleted = true;
                        break;
                    case WorkflowActivityType.Form:
                        // send form notification to owners
                        await SendFormWorkflowEmail(itemStep, itemStepID, itemID, objectInfo, stepSettings);
                        break;
                    case WorkflowActivityType.StatusChange:
                        // change the status of this item
                        ChangeItemStatus(itemStep.Step,objectInfo);
                        isStepCompleted = true;
                        break;
                    case WorkflowActivityType.Procedure:
                        // execute proc with specified id
                        ExecuteProc(itemStep, objectInfo, stepSettings);
                        isStepCompleted = true;
                        break;
                    case WorkflowActivityType.FieldChange:
                        // change the specified field and mark the step complete
                        UpdateItemField(itemStep, objectInfo, stepSettings);
                        isStepCompleted = true;
                        break;
                    case WorkflowActivityType.RelationshipChange:
                        UpdateItemRelationship(itemStep, objectInfo, stepSettings);
                        isStepCompleted = true;
                        break;
                    case WorkflowActivityType.Delete:
                        DeleteItemWorkflowActivity(objectInfo);
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
                await MarkStepAsCompleteAndContinue(itemStep, itemID, objectInfo);
            }
        }

        private void DeleteItemWorkflowActivity(EventObjectInfo objectInfo)
        {
            Delete(objectInfo.Object, objectInfo.ObjectID);
        }

        private void UpdateItemRelationship(WorkflowItemStep itemStep, EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
        {
            if (!settings.RelationshipUpdateSettings.Any())
                throw new Exception($"ERROR - INVALID RELATIONSHIP UPDATE SETTINGS SPECIFIED.");

            foreach (var item in settings.RelationshipUpdateSettings)
            {
                if (string.IsNullOrEmpty(item.FormField) || item.FormStepID <= 0)
                    throw new Exception($"ERROR - INVALID FORM FIELD OR FORM STEP ID SPECIFIED FOR RELATIONSHIP UPDATE STEP.  FORM FIELD IS : [{item.FormField}] FORM STEP IS : [{item.FormStepID}]");

                var verStep = WorkflowVersionSteps.Where(x => x.ID == item.FormStepID).FirstOrDefault();

                if(verStep == null)
                    throw new Exception($"ERROR - FORM STEP TO USE AS THE SOURCE OF THE RELATIONSHIP IS INVALID AND CANNOT BE LOADED");

                //load the intersect from the form fields
                //XDocument.Parse(verStep.Fields)
                var formFields = WorkflowFormFormModel.ParseXml(XElement.Parse(verStep.Fields).Element("form"));

                var field = formFields.Fields.Where(x => x.ID == item.FormField).FirstOrDefault();

                if (field == null)
                    throw new Exception($"ERROR - CANNOT FIND FORM FIELD TO USE AS INPUT FOR RELATIONSHIP.");

                if (!int.TryParse(field.IntersectTypeID, out int intersectTypeId))
                    throw new Exception("ERROR - CANNOT PARSE THE INTERSECTTYPEID VALUE AS AN INTEGER");

                //get intersect type info
                var intersectType = IntersectTypes.Where(x => x.ID == intersectTypeId).FirstOrDefault();

                if (intersectType == null)
                    throw new Exception($"ERROR - INVALID INTERSECT TYPE ID SPECIFIED.  PLEASE CHECK THE SETTINGS ASSOCIATED WITH THE RELATIONSHIP UPDATE ACTION OF THE CURRENT WORKFLOW. INTERSECT TYPE ID IS [{intersectTypeId}]");

                var isSubject = intersectType.SubjectID == objectInfo.ObjectID && intersectType.Subject == objectInfo.Object.ToString();

                if (item.ClearValue)
                {
                    //delete intersects with the given intersect type id for the current object
                    DeleteIntersects(objectInfo.Object, objectInfo.ObjectID, intersectTypeId, isSubject);
                }
                else
                {
                    var val = GetFieldValueIntersectFromFormResponse(item, itemStep.ItemID);

                    bool supportsJustOne = isSubject ? intersectType.ObjectCardinality == core.enums.Cardinality.One : intersectType.SubjectCardinality == core.enums.Cardinality.One;

                    if (!item.AppendValue || supportsJustOne)
                    {
                        DeleteIntersects(objectInfo.Object, objectInfo.ObjectID, intersectTypeId, isSubject);
                    }

                    //split the value on , 
                    var rels = val.Split(',');

                    // if it just supports one item just use the first selected.
                    if(supportsJustOne)
                        rels = rels.Where((v, idx) => idx != 0).ToArray();

                    foreach (var rel in rels)
                    {
                        // split by | for type id
                        // add item
                        var parts = rel.Split('|');
                        
                        var intersect = new Intersect();

                        intersect.IntersectTypeID = intersectType.ID;

                        if (isSubject) {
                            intersect.Subject = objectInfo.Object.ToString();
                            intersect.SubjectID = objectInfo.ObjectID;
                            intersect.Object = (parts[0] ?? "").Replace("Type", "");
                            intersect.ObjectID = int.Parse(parts[1]);
                        }
                        else
                        {
                            intersect.Object = objectInfo.Object.ToString();
                            intersect.ObjectID = objectInfo.ObjectID;
                            intersect.Subject = (parts[0] ?? "").Replace("Type", "");
                            intersect.SubjectID = int.Parse(parts[1]);
                        }

                        //check that this relationship doesnt already exist


                        if (!Intersects.Any(x => x.IntersectTypeID == intersectTypeId && x.Subject == intersect.Subject && x.SubjectID == intersect.SubjectID && x.Object == intersect.Object && x.ObjectID == intersect.ObjectID))
                        {

                            intersect.CreatedBy = CurrentResourceID;
                            intersect.CreatedOn = DateTime.UtcNow;
                            intersect.UpdatedBy = CurrentResourceID;
                            intersect.UpdatedOn = DateTime.UtcNow;

                            Intersects.Add(intersect);

                            SaveChanges();
                        }
                        
                    }
                }
            }
        }

        private void DeleteIntersects(SystemObjects @object, int objectID, int intersectTypeId, bool isSubject)
        {
            var sql = "";

            if (isSubject)
                sql = "delete [intersect] where subject = @obj and subjectid = @objectid and intersecttypeid = @intersectTypeId";
            else
                sql = "delete [intersect] where [object] = @obj and objectid = @objectid and intersecttypeid = @intersectTypeId";

            Query<int>(sql, new { obj = @object.ToString(), objectid = objectID, intersectTypeId = intersectTypeId });
        }

        private void UpdateItemField(WorkflowItemStep itemStep, EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
        {
            if (!settings.FieldUpdateSettings.Any()) return;

            foreach (var item in settings.FieldUpdateSettings)
            {
                // get field type info
                var fieldType = FieldTypes.Where(x => x.ID == item.FieldID).FirstOrDefault();

                if (fieldType == null)
                    throw new Exception($"ERROR - INVALID FIELD TYPE ID SPECIFIED FOR UPDATE FIELD WORKFLOW TASK. FIELD ID[ {item.FieldID} ]");

                if (item.ClearValue)
                {
                    //delete the value
                    var sql = "delete field where objectid = @id and objecttype = @objectType and fieldtypeid = @fieldTypeId";

                    Query<int>(sql, new { id = objectInfo.ObjectID, objectType = objectInfo.Object.ToString(), fieldTypeId = item.FieldID });

                }
                else
                {
                    var val = item.CurrentDate ? DateTime.UtcNow.ToShortDateString() : item.Value;
                    //if the value is a form value get it
                    if (item.UseFormValue && !string.IsNullOrEmpty(item.FormField) && item.FormStepID > 0)
                    {
                        val = GetFieldValueFromFormResponse(item, itemStep.ItemID);
                    }

                    // check if the field exists
                    var field = Fields.Where(x => x.ObjectID == objectInfo.ObjectID && x.ObjectType == objectInfo.Object.ToString() && x.FieldTypeID == fieldType.ID).FirstOrDefault();

                    if (field == null)
                    {
                        //insert
                        var newField = new core.entities.Field
                        {
                            Value = val,
                            FieldTypeID = fieldType.ID,
                            ObjectID = objectInfo.ObjectID,
                            ObjectType = objectInfo.Object.ToString()
                        };

                        Add<core.entities.Field>(newField);

                        SaveChanges();
                    }
                    else
                    {
                        if (item.AppendValue)
                        {
                            if (field.Value.EndsWith(","))
                                field.Value += val.Trim(',');
                            else
                                field.Value += ("," + val.Trim(','));                            
                        }
                        //update
                        field.Value = val;

                        SaveChanges();
                    }
                }

            }
        }

        private string GetFieldValueIntersectFromFormResponse(WorkflowRelationshipUpdateSettings item, long itemId)
        {
            var formResponses = WorkflowItemSteps.Where(x => x.ItemID == itemId && x.StepID == item.FormStepID && x.Step.ActivityType == WorkflowActivityType.Form);

            var firstResponse = formResponses.FirstOrDefault();

            if (firstResponse == null) return string.Empty;

            var xml = XElement.Parse(firstResponse.Fields);

            foreach (var form in xml.Elements("form"))
            {
                foreach (var field in form.Elements("field"))
                {
                    if ((string)field.Attribute("id") == item.FormField)
                        return (string)field.Attribute("value");
                }
            }

            return "";
        }

        private string GetFieldValueFromFormResponse(WorkflowFieldUpdateSettings item, long itemId)
        {
            var formResponses = WorkflowItemSteps.Where(x => x.ItemID == itemId && x.StepID == item.FormStepID && x.Step.ActivityType == WorkflowActivityType.Form);

            var firstResponse = formResponses.FirstOrDefault();

            if (firstResponse == null) return string.Empty;

            var xml = XElement.Parse(firstResponse.Fields);

            foreach (var form in xml.Elements("form"))
            {
                foreach (var field in form.Elements("field"))
                {
                    if((string)field.Attribute("id") == item.FormField)
                        return (string)field.Attribute("value");                    
                }
            }

            return "";
        }

        private void ExecuteProc(WorkflowItemStep itemStep, EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
        {
            if(settings.StoredProcedureID <= 0)
            {
                Console.WriteLine($"DEBUG : STORED PROC STEP DOESNT HAVE A VALID PROCEDURE ID.");

                return;
            }

            var procInfo = WorkflowTaskProcedures.Where(x => x.ID == settings.StoredProcedureID).FirstOrDefault();


            if (!procInfo.PassObjectInfo)
            {
                Console.WriteLine($"DEBUG : EXECUTING PROCEDURE ID[{procInfo.ID}] PROC[{procInfo.Name}].  NOT PASSING OBJECT INFO.");

                Database.Connection.Execute($"{procInfo.Procedure}", commandType: System.Data.CommandType.StoredProcedure);
            }
            else
            {
                Console.WriteLine($"DEBUG : EXECUTING PROCEDURE ID[{procInfo.ID}] PROC[{procInfo.Name}].  PASSING OBJECT INFO.");

                Database.Connection.Execute($"{procInfo.Procedure} @obj,@objectId", new { obj = objectInfo.Object.ToString(), objectId = objectInfo.ObjectID });
            }
        }

        private void SaveItemAssignments(IEnumerable<core.entities.GlobalReportingResource> users, long itemId)
        {
            foreach (var user in users)
            {
                var assignment = new WorkflowItemAssignment
                {
                    CreatedBy = 0,
                    CreatedOn = DateTime.UtcNow,
                    ItemID = itemId,
                    ResourceObject = "Resource",
                    ResourceObjectID = user.ResourceID,
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
                    //artifact.Status = statusModel.Status;
                    SaveChanges();
                    break;
            }

        }

        private void SetObjectVisibility(EventObjectInfo objectInfo, bool visibility = true)
        {
            Console.WriteLine($"Debug - Setting Object {objectInfo.Object} {objectInfo.ObjectID} as visible {visibility}");

            switch (objectInfo.Object)
            {
                case core.SystemObjects.Artifact:
                    var artifact = Artifacts.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (artifact == null) return;
                    artifact.Visible = visibility;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Intersect:
                    var intersect = Intersects.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (intersect == null) return;
                    //intersect.Visible = visibility;
                    intersect.State = visibility ? State.Active : State.PendingAdd;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Taxonomy:
                    var taxonomy = Taxonomies.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (taxonomy == null) return;
                    taxonomy.Visible = visibility;
                    SaveChanges();
                    break;                                
                case core.SystemObjects.Policy:
                    var policy = Policies.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (policy == null) return;
                    policy.Visible = visibility;
                    SaveChanges();
                    break;                
                case core.SystemObjects.Rule:
                    var rule = Rules.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();
                    if (rule == null) return;
                    rule.Visible = visibility;
                    SaveChanges();
                    break;                                             
                default:
                    break;
            }
        }

        public void RequestObjectCertification(core.SystemObjects @object, int objectId, core.SystemObjects objectType, int objectTypeId)
        {
            var events = new List<EventInfo>();

            events.Add(new EventInfo
            {
                CompanyID = CurrentCompanyID,
                DomainPrefix = CurrentCompanyDomain,
                ResourceID = CurrentResourceID,                
                Action = ChangeType.RequestCertification,
                Object = new EventObjectInfo
                {
                    Object = @object,
                    ObjectID = objectId,
                    ObjectType = objectType,
                    ObjectTypeID = objectTypeId
                }
            });
                        
            QueueSource.CreateTopicMessages(events);
        }

        public async Task MarkStepAsCompleteAndContinue(WorkflowItemStep itemStep, long itemID, EventObjectInfo objectInfo)
        {
            // mark step as completed
            itemStep.CompletedOn = DateTime.UtcNow;
            itemStep.CompletedBy = CurrentResourceID;
            SaveChanges();

            // get the transitions for this step and add events
            var transitions = WorkflowVersionStepTransitions
            .Where(i => i.FromVersionStepID == itemStep.StepID && i.TransitionType != TransitionType.Timer)
            .ToList();

            if(transitions.Count > 0)
                await StartTransitions(transitions, itemID, objectInfo);
        }

        private async Task SendFormWorkflowEmail(WorkflowItemStep item, long itemStepID, long itemId, EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
        {
            List<string> emailedUsers = new List<string>();
            List<core.entities.GlobalReportingResource> users = new List<core.entities.GlobalReportingResource>();
            //based on the step settings get the users

            if (settings.RecipientType == EmailTaskRecipientType.Initiator)
            {
                if (item.Item.StartedBy <= 0)
                {
                    Console.WriteLine("ERROR CANNOT DETERMINE WHO TO ASSIGN FORM STEP TO.");

                    return;
                }

                var res = GlobalReportingResources.Where(x => x.ResourceID == item.Item.StartedBy).FirstOrDefault();

                if (res == null)
                {
                    Console.WriteLine("ERROR CANNOT FIND THE RESOURCE WHO STARTED THE WORKFLOW TO ASSIGN FORM TO.");

                    return;
                }

                users.Add(res);

                Console.WriteLine($"DEBUG : FORM STEP IS ASSIGNED TO [{res.Email}].");
                emailedUsers.Add(res.Email);
            }
            else if (settings.RecipientType == EmailTaskRecipientType.Responsibility || settings.RecipientType == EmailTaskRecipientType.None)
            {
                users = Query<core.entities.GlobalReportingResource>("[utility].[GetOwnersForWorkflow] @id, @stepId, @itemId", new { id = item.Step.Version.TypeID, @stepId = item.Step.ID, @itemId = item.ItemID }).ToList();
            }
            else if(settings.RecipientType == EmailTaskRecipientType.SpecificUser)
            {
                if (string.IsNullOrEmpty(settings.SpecificUser))
                {
                    Console.Write("ERROR - NO USER SPECIFIED FOR THE SPECIFIC USER FORM TASK.");

                    return;
                }

                Console.WriteLine($"DEBUG : FORM STEP IS ASSIGNED TO [{settings.SpecificUser}].");

                foreach (var email in settings.SpecificUser.Split(';'))
                {
                    emailedUsers.Add(email);

                    var res = GlobalReportingResources.Where(x => string.Compare(x.Email, email, true) == 0).FirstOrDefault();

                    if (res == null)
                    {
                        Console.WriteLine("FORM EMAIL SPECIFIC USER SET HOWEVER THE USER EMAIL IS NOT A VALID D3S EMAIL ACCOUNT.  WONT BE ABLE TO ASSIGN FORM TO USER..");

                        continue;
                    }

                    users.Add(res);
                }
            }
                        
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

            if (settings.FormShouldSendEmail)
            {
                var obj = GetObjectDetail(objectInfo.Object.ToString(), objectInfo.ObjectID);

                var itemName = (obj == null) ? "(unknown)" : obj.Name;
                var emailSubject = $"Data3Sixty - Workflow [{item.Step.Version.Type.Name}] - Form";
                var emailBody = $"<p>The Data3Sixty workflow <b>{item.Step.Version.Type.Name}</b> has generated a form that you need to complete for the item <b>{itemName}</b>.  This workflow was initiated by {initiatedBy}.  Please complete the form at {url}</p>";

                var customBody = ProcessMessageBody(settings.BodyTemplate, objectInfo, prefix, item);

                if (!string.IsNullOrEmpty(customBody))
                {
                    emailBody = $"{customBody} <br>Please complete the form at {url}";
                }

                var emailBase = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\"><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"width: 100%; background-color: #54a4da\"><tbody><tr><td><span style=\"float: none; display: inline-block; text-align: left;\"><img alt=\"Data3Sixty, Inc.\" height=\"50\" src=\"https://d3spublic.blob.core.windows.net/images/Logo246x50.jpg\" width=\"246\"></span></td></tr></tbody></table>{emailBody}</body></html>";

                foreach (var user in users)
                {
                    Console.WriteLine($"DEBUG : FORM STEP EMAIL IS EMAILING [{user.Email}].");

                    emailedUsers.Add(user.Email);

                    await extensions.mail.SimpleMessage.SendMessage(emailSubject, (string)user.Email, (string)user.FirstName + " " + (string)user.LastName, emailBase, true);
                }

                SaveItemStepEmailedUsers(item, emailedUsers);
            }

            SaveItemAssignments(users, itemId);
        }

        private async Task SendAggregateWorkflowEmail(WorkflowEventRegistrationSettingsModel settings, List<string> items)
        {
            
            var url = "";
            var prefix = "";
            using (var cnn = new System.Data.SqlClient.SqlConnection(core.constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();

                prefix = cnn.Query<string>(@"select UrlPrefix from CompanyDomainSetting where CompanyID = @c and IsPrimary = 1", new { c = CurrentCompanyID }).FirstOrDefault();

                cnn.Close();
            }

            settings.EmailMessageTemplate = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\"><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"width: 100%; background-color: #54a4da\"><tbody><tr><td><span style=\"float: none; display: inline-block; text-align: left;\"><img alt=\"Data3Sixty, Inc.\" height=\"50\" src=\"https://d3spublic.blob.core.windows.net/images/Logo246x50.jpg\" width=\"246\"></span></td></tr></tbody></table>{settings.EmailMessageTemplate}";

            //if (items.Any()) settings.EmailMessageTemplate += "<b>The following items triggered this workflow:</b></br>";

            /*foreach (var item in items)
            {
                settings.EmailMessageTemplate += $"<br>{item}";
            }*/
            settings.EmailMessageTemplate += "</body></html>";

            if (settings.RecipientType == EmailTaskRecipientType.SpecificUser)
            {
                if (string.IsNullOrEmpty(settings.SpecificUser))
                {
                    Console.Write("ERROR - NO USER SPECIFIED FOR THE SPECIFIC USER EMAIL TASK.");

                    return;
                }

                foreach (var email in settings.SpecificUser.Split(';'))
                {

                    Console.WriteLine($"DEBUG : WORKFLOW AGGREGATE EMAIL IS EMAILING [{email}].");

                    await extensions.mail.SimpleMessage.SendMessage(settings.EmailHeader, email, "", settings.EmailMessageTemplate, true);
                }
            }
        }

        private async Task SendWorkflowEmail(WorkflowItemStep item, EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
        {
            List<string> emailedUsers = new List<string>();

            if (string.IsNullOrEmpty(item.Step.Settings)) throw new Exception("INVALID EMAIL CONFIGURATION FOR SPECIFIED STEP.");
            
            var url = "";
            var prefix = "";
            using (var cnn = new System.Data.SqlClient.SqlConnection(core.constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();

                prefix = cnn.Query<string>(@"select UrlPrefix from CompanyDomainSetting where CompanyID = @c and IsPrimary = 1", new { c = CurrentCompanyID }).FirstOrDefault();

                cnn.Close();
            }

            url += $"https://{prefix}.data3sixty.com/workflow/details/{item.ItemID}";

            settings.BodyTemplate = ProcessMessageBody(settings.BodyTemplate, objectInfo, prefix, item);

            settings.BodyTemplate = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\"><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\"width: 100%; background-color: #54a4da\"><tbody><tr><td><span style=\"float: none; display: inline-block; text-align: left;\"><img alt=\"Data3Sixty, Inc.\" height=\"50\" src=\"https://d3spublic.blob.core.windows.net/images/Logo246x50.jpg\" width=\"246\"></span></td></tr></tbody></table>{settings.BodyTemplate}<p>Item Workflow Details {url}</p>";

            //if the setting to include responses from froms is enabled then get previous form responses and put in xml
            if (settings.ShouldIncludeFormResponses)
            {
                var formResponses = WorkflowItemSteps.Where(x => x.ItemID == item.ItemID && x.Step.ActivityType == WorkflowActivityType.Form);

                StringBuilder sb = new StringBuilder();
                sb.Append($"<br><br><b>Form responses</b><br>");

                foreach(var formResponse in formResponses)
	            {
                    if (string.IsNullOrEmpty(formResponse.Fields)) continue;

                    var xml = XElement.Parse(formResponse.Fields);
                    
                    foreach (var form in xml.Elements("form"))
                    {
                        int resourceID = 0;
                                         
                        if(int.TryParse((string)form.Attribute("ResourceID"), out resourceID))
                        {
                            var user = GlobalReportingResources.Where(x => x.ResourceID == resourceID).FirstOrDefault();

                            if(user != null)
                            {
                                sb.Append($"<br>Response from user <b>{user.FullName}</b><br>");
                            }
                        }

                        foreach(var field in form.Elements("field"))
                        {
                            var fieldName = (string)field.Attribute("label");
                            var value = (string)field.Attribute("value");
                            var fieldType = (string)field.Attribute("fieldtype");

                            if((fieldType ?? "").ToUpper() == "LIST")
                            {
                                value = (string)field.Attribute("displayvalue");
                            }

                            sb.Append($"<b>{fieldName}</b> {value}<br>");
                        }
                    }

                    settings.BodyTemplate += sb.ToString();
	            }
            }

            settings.BodyTemplate += "</body></html>";

            if (settings.RecipientType == EmailTaskRecipientType.Initiator)
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

                Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{res.Email}].");
                emailedUsers.Add(res.Email);

                await extensions.mail.SimpleMessage.SendMessage(settings.SubjectTemplate, (string)res.Email, (string)res.FirstName + " " + (string)res.LastName, settings.BodyTemplate, true);
            }
            else if(settings.RecipientType == EmailTaskRecipientType.Responsibility)
            {
                var users = Query<dynamic>("[utility].[GetOwnersForWorkflow] @id, @stepId", new { id = item.Step.Version.TypeID, @stepId = item.Step.ID });

                foreach (var user in users)
                {
                    Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{user.Email}].");

                    emailedUsers.Add(user.Email);

                    await extensions.mail.SimpleMessage.SendMessage(settings.SubjectTemplate, (string)user.Email, (string)user.FirstName + " " + (string)user.LastName, settings.BodyTemplate, true);
                }
            }
            else if(settings.RecipientType == EmailTaskRecipientType.SpecificUser)
            {
                if(string.IsNullOrEmpty(settings.SpecificUser))
                {
                    Console.Write("ERROR - NO USER SPECIFIED FOR THE SPECIFIC USER EMAIL TASK.");

                    return;
                }

                Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{settings.SpecificUser}].");

                foreach (var email in settings.SpecificUser.Split(';'))
                {
                    emailedUsers.Add(email);

                    await extensions.mail.SimpleMessage.SendMessage(settings.SubjectTemplate, email, "", settings.BodyTemplate, true);
                }
            }

            SaveItemStepEmailedUsers(item, emailedUsers);
        }

        private void SaveItemStepEmailedUsers(WorkflowItemStep item, List<string> emailedUsers)
        {

            //save the emailed users to the settings
            if (emailedUsers.Count > 0 && !string.IsNullOrEmpty(item.Settings))
            {
                var root = XElement.Parse(item.Settings);

                var emailForm = new XElement("emails");

                foreach (var email in emailedUsers)
                {
                    emailForm.Add(new XElement("email",
                            new XAttribute("address", email)));
                }

                root.Add(emailForm);

                item.Settings = root.ToString();
                SaveChanges();
            }
        }

        private string ProcessMessageBody(string bodyTemplate, EventObjectInfo objectInfo, string prefix, WorkflowItemStep itemStep)
        {
            var result = bodyTemplate;
            //replace [OBJECT_NAME] with the object name            
            if (result.Contains("[OBJECT_NAME]"))
            {
                //get the objects name
                var item = GetObjectDetail(objectInfo.Object.ToString(), objectInfo.ObjectID);
                var itemLink = "(unknown item)";

                if (item != null)                
                    itemLink = $"<b><a href=\"https://{prefix}.data3sixty.com/{item.Url}\">{item.Name}</a></b>";

                result = result.Replace("[OBJECT_NAME]", itemLink);
            }

            //if (result.Contains("[OBJECT_TAXONOMY]") && objectInfo.Object == core.SystemObjects.Artifact)
            //{
            //    //get the objects name
            //    var artifact = Artifacts.Where(i => i.ID == objectInfo.ObjectID).Include(x => x.TaxonomyType).FirstOrDefault();
                
            //    var taxonomy = "(unknown)";

            //    if (artifact != null)
            //        taxonomy = artifact.TaxonomyType.Name;

            //    result = result.Replace("[OBJECT_TAXONOMY]", taxonomy);
            //}

            if (result.Contains("[ACTION_DETAILS]"))
            {
                //get the details of the issue and add them in
                var issue = Issues.Where(i => i.ID == objectInfo.ObjectID).Include(x => x.IssueType).FirstOrDefault();

                var issueInfo = "(unknown)";

                if (issue != null)
                {
                    var item = GetObjectDetail(issue.Object, issue.ObjectID);

                    if(item != null)
                        issueInfo = $"New Action Type <b>{issue.IssueType.Name}</b> Raised on <b>{item.Name}</b>.  <br>Criticality Level: {issue.Criticality}";

                    var creator = GlobalReportingResources.Where(x => x.ResourceID == issue.CreatedBy).FirstOrDefault();

                    if (creator != null)
                        issueInfo += $"<br>Created By <b>{creator.FullName}</b>";
                    
                    //get any field values for this issue
                    var fieldTypes = FieldTypes.Where(x => x.Object == "IssueType" && x.ObjectID == issue.IssueTypeID);

                    var fieldValues = Fields.Where(x => x.ObjectType == "Issue" && x.ObjectID == issue.ID);

                    foreach (var fieldType in fieldTypes)
                    {
                        var field = fieldValues.Where(x => x.FieldTypeID == fieldType.ID).FirstOrDefault();

                        if(field != null)
                        {
                            issueInfo += $"<br><b>{fieldType.FriendlyName}</b>: {field.FormattedValue}";
                        }
                    }
                }

                result = result.Replace("[ACTION_DETAILS]", issueInfo);
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

            if (result.Contains("[SCORE]"))
            {
                var score = objectInfo.Score.HasValue ? "(unknown score)" : objectInfo.Score.Value.ToString();
                
                result = result.Replace("[SCORE]", score);
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

                        if(fieldRecord != null)
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
