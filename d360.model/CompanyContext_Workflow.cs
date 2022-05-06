using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

using d360.core;
using d360.core.entities;
using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.model.workflow;

using Dapper;

using Microsoft.ApplicationInsights;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d360.model
{
	public partial class CompanyContext : BaseContext
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
			string workflowName = "";
			
			if (registration.Type != null)
			{
				workflowName = registration.Type.Name;
			}

			Console.WriteLine($"DEBUG - TESTING TO SEE IF ${objectInfo.Object} - {objectInfo.ObjectID} IS VALID FOR WORKFLOW {workflowName}");

			string issueObjectType = "";
			int issueObjectId = -1;

			core.entities.Workflow.Type workflowType = WorkflowTypes.Where(x => x.ID == registration.TypeID).FirstOrDefault();
			
			if (workflowType.State != State.Active)
			{
				return false;
			}

			if (objectInfo.Object == SystemObjects.Issue)
			{
				Console.WriteLine($"DEBUG - WORKFLOW IS AN ISSUE.  DETERMINING WHAT OBJECT THE ISSUE WITH ID {objectInfo.ObjectID} WAS RAISED ON.");

				Issue issueDetail = Issues.Where(x => x.ID == objectInfo.ObjectID).FirstOrDefault();

				if (issueDetail == null)
				{
					Console.WriteLine("ERROR - ISSUE RAISED BUT DOESNT HAVE CORRESPONDING ISSUE RECORD.");

					return false;
				}

				issueObjectType = issueDetail.ObjectType;
				issueObjectId = issueDetail.ObjectTypeID;
			}

			if (!WorkflowRegistrationCriteriaProcessor.Evaluate(this, objectInfo.Object.ToString(), objectInfo.ObjectID, registration.Condition, -1, objectInfo.ChangedFieldIds, issueObjectType, issueObjectId, (int?)objectInfo.ScoreType))
			{
				Console.WriteLine("DEBUG - CURRENT ITEM DOESNT MATCH CRITERIA FOR THE WORKFLOW");

				return false;
			}

			Console.WriteLine("DEBUG - OBJECT MATCHES SPECIFIED CRITERIA");

			return true;
		}

		public async Task SendDigestEmails(EnvironmentLevel environmentLevel)
		{
			List<SettingInfo> companySettings = GetSettings();

			// 0 check if the workflow digest emails are enabled for today
			int digestDays = int.TryParse(companySettings.First(s => s.ID == Setting.WorkflowDigestEmailDays).Value, out digestDays) ? digestDays : 0;
			int todayDayOfWeek = (int)DateTime.UtcNow.DayOfWeek;

			//Check if today is a day to send digest emails
			if (((int)Math.Pow(2, todayDayOfWeek) & digestDays) == 0)
			{
				return;
			}

			// 0.5 determine how many days ago last digest was sent
			int newDelta = 0;
			int previousDayOfWeek;
			do
			{
				newDelta++;
				previousDayOfWeek = (7 + todayDayOfWeek - newDelta) % 7;
			} while (((int)Math.Pow(2, previousDayOfWeek) & digestDays) == 0);


			// 1 determine which users have outstanding workflows
			IEnumerable<dynamic> users = await GetUsersWithOutstandingWorkflows();

			// 2 loop through the users with outstanding workflows and create an email for each
			if (users != null && users.Any())
			{
				string fromName = companySettings.First(s => s.ID == Setting.WorkflowFromName).Value;
				string fromEmail = companySettings.First(s => s.ID == Setting.WorkflowFromEmail).Value;

				string tblHeader = string.Empty;
				string tblTR = string.Empty;
				string span = string.Empty;
				string tblTRWhite = string.Empty;

				#region table formating

				tblHeader = @"<table style='width: 692px; border-style: none; border-width: 0px; box-sizing: border-box; line-height: 18px;'><thead style='background-color: #252c41; '>
					<tr>
					<th style='text-align:left;padding-left:5px;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif;color:#FFF;'>Name</th>
					<th style='text-align:center;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif;color:#FFF;'>Version</th>
					<th style='text-align:left;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif;color:#FFF;'>Step</th>
					<th style='text-align:center;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif;color:#FFF;'>New</th>
					<th style='text-align:center;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif;color:#FFF;'>Total</th>

					</tr>
					</thead>
					<tbody>";

				tblTR = @"<tr style='background-color: #f1f1f1; box-sizing: border-box; color: #646464; display: table-row; border: 0px none #d9d9d9;'>";

				tblTRWhite = @"<tr style='background-color: #FFF; box-sizing: border-box; color: #646464; display: table-row; border: 0px none #d9d9d9;'>";

				span = @"<span style='border-collapse: collapse; box-sizing: border-box; color: #646464; display: inline; font-family: Trebuchet MS, Arial, Helvetica,sans-serif; font-size: 12px; font-weight: 400; height: auto; line-height: 18px;text-size-adjust:100%;width: auto; word-wrap: break-word;'>";

				string rootUrl = $"https://{Community.GetPrimaryUrlPrefix()}.data3sixty.com";

				#endregion

				// 3 get oustanding assignments
				foreach (dynamic user in users)
				{
					dynamic workflows = await GetUsersOutstandingWorkflows(user.ID, newDelta);


					StringBuilder sb = new StringBuilder();
					string subject = string.Empty;
					string environment = string.Empty;

					//build email content
					// email summary
					sb.Append("<span style='padding-left:5px;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif;color:#0000;'>Please find listed below the outstanding workflow items assigned to you:</span>");
					
					// email details
					if (environmentLevel != EnvironmentLevel.Production)
					{
						sb.Append($"<br><span style='padding-left:5px;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif;color:#0000;'>({environmentLevel.ToString()}  environment)</span><br><br>");
						environment = $"[{environmentLevel.ToString()}] ";
					}
					else
					{
						sb.Append("<br><br>");
					}
					sb.Append(tblHeader);
					int i = 0;
					int totalNew = 0;
					foreach (dynamic item in workflows)
					{
						if (i % 2 == 0)
						{
							sb.Append(tblTR);
						}
						else
						{
							sb.Append(tblTRWhite);
						}

						string url = $"{rootUrl}/workflow/workflowlistnew/{item.Id}/{item.Version}/{item.StepId}/1";
						sb.Append($"<td style='text-align: left;padding-left:5px;'><a style='font-size:12px;font-family: Trebuchet MS, Arial, Helvetica, sans - serif;'  href='{url}'>{item.Name}</a></td>");
						sb.Append($"<td style='text-align: center'>{span}{item.Version}</span></td>");
						sb.Append($"<td style='text-align: left'>{span}{item.Step}</span></td>");
						sb.Append($"<td style='text-align: center'>{span}{item.New}</span></td>");
						sb.Append($"<td style='text-align: center'>{span}{item.Total}</span></td>");

						sb.Append("</tr>");
						i++;
						totalNew += item.New;

					}

					if (i == 0)
					{
						continue;
					}

					sb.Append("</tbody></table>");

					sb.Append($"<p style='margin-top:20px;'><a href='{rootUrl}/home' style='padding-left:5px;font-size:12px;font-weight:700;font-family: Trebuchet MS, Arial, Helvetica, sans-serif'>View all workflow assignments</a></p>");

					subject = $"{environment}{totalNew} new workflow items require your attention";

					dynamic emailAddress = user.Email;

					string emailBase = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family: Trebuchet MS, Arial, Helvetica, sans-serif;\">{sb.ToString()}</body></html>";
					
					//send email
					await Mail.SendMessage(subject, emailAddress, "", emailBase, true, fromEmail, fromName);
				}
			}
		}

		private async Task<IEnumerable<dynamic>> GetUsersWithOutstandingWorkflows()
		{
			return await Database.Connection.QueryAsync<dynamic>(@"select distinct WIA.ResourceObjectID as ID, GRAA.Email
																from
																	[workflow].[Type] WT
																	inner join workflow.[Version] WV on WT.ID = WV.TypeID
																	inner join workflow.Item WI on WV.ID = WI.VersionID
																	inner join reporting.Global_Resource GR on WI.StartedBy = GR.ResourceID									
																	inner join workflow.ItemStep WIS on WIS.ItemID = WI.ID and WIS.CompletedOn is null
																	inner join workflow.ItemAssignment WIA on WIA.ItemID = WI.ID and WIA.ResourceObject = 'Resource'
																	inner join workflow.VersionStep WVS on WVS.ID = WIS.StepID                                    
																	inner join reporting.Global_Resource GRAA on WIA.ResourceObjectID = GRAA.ResourceID									
																where
																		WI.CompletedOn is null and WVS.StepType = 2 and WVS.ActivityType = 3 and GRAA.State = 1");
		}

		private async Task<IEnumerable<dynamic>> GetUsersOutstandingWorkflows(int resourceId, int newOffset = 1)
		{
			return await Database.Connection.QueryAsync<dynamic>(@"
					Select wfm.Name as Name,wfm.Id as Id,wfm.Version as Version,wfm.Step as Step,wfm.StepId as StepId,wfm.Total as Total,Isnull(Sub.New,0) as New
					from(
					select 
					wt.name as Name
					,wt.id as Id
					,wv.[version] as Version
					, wvs.name as Step
					,wvs.Id as StepId
					,count(1) as Total 
					from
					[workflow].[type] wt
					inner join [workflow].[version] wv on (wt.id = wv.typeid)
					inner join [workflow].[item] wi on (wv.id = wi.versionid)	
					inner join [workflow].[itemstep] wis on(wis.itemid = wi.id and wis.completedon is null)
					inner join [workflow].[itemassignment] wia on(wia.itemid = wi.id and wia.resourceobject = 'Resource' and wia.resourceobjectid = @r)
					inner join [workflow].[versionstep] wvs on(wvs.id = wis.stepid)
					where
					wi.completedon is null and wvs.steptype = 2 and wvs.activitytype = 3
					group by wt.name, wt.id,wv.[version],wvs.name,wvs.Id 
					) as wfm
					left join
					(
					select 
	
					wt.id as Id
					,wv.[version] as Version
					, wvs.name as Step
					,wvs.Id as StepId
					,count(1) as New
					from
					[workflow].[type] wt
					inner join [workflow].[version] wv on (wt.id = wv.typeid)
					inner join [workflow].[item] wi on (wv.id = wi.versionid)	 
					inner join [workflow].[itemstep] wis on(wis.itemid = wi.id and wis.completedon is null)
					inner join [workflow].[itemassignment] wia on(wia.itemstepid = wis.id and wia.resourceobject = 'Resource' and wia.resourceobjectid = @r)
					inner join [workflow].[versionstep] wvs on(wvs.id = wis.stepid)
					where
					wi.completedon is null and wvs.steptype = 2 and wvs.activitytype = 3  and wia.CreatedOn > getdate()-@newOffset
					group by wt.name, wt.id,wv.[version],wvs.name,wvs.Id
					) as Sub on
					wfm.Id =Sub.Id and wfm.Version=Sub.Version and wfm.StepId = sub.stepid
					order by wfm.Name asc,wfm.[version] desc,wfm.Step asc", new { r = resourceId, newOffset });
		}

		public Issue AssignActivityWorkflowToNewObject(WorkflowEventRegistration reg, int itemId, int workflowId, int objectId, string @object)
		{
			WorkflowItem item = WorkflowItems.Where(x => x.ID == itemId).FirstOrDefault();

			if (item == null)
			{
				throw new ArgumentNullException(nameof(item), "invalid workflow item");
			}

			Issue orgIssue = Issues.Where(x => x.ID == item.ObjectID).FirstOrDefault();

			if (orgIssue == null)
			{
				throw new ArgumentNullException(nameof(orgIssue), "invalid workflow issue");
			}

			ObjectDetail obj = GetObjectDetail(@object, objectId);

			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj), "Unable to find object details of object to reassign to");
			}

			//add new issue record
			Issue issue = new Issue
			{
				Object = @object,
				ObjectID = objectId,
				ObjectType = obj.Type,
				ObjectTypeID = obj.TypeID,
				CreatedBy = CurrentResourceID,
				CreatedOn = DateTime.UtcNow,
				UpdatedBy = CurrentResourceID,
				UpdatedOn = DateTime.UtcNow,
				IssueTypeID = orgIssue.IssueTypeID,
				CommentID = orgIssue.CommentID,
				InitiatorID = item.StartedBy
			};

			Issues.Add(issue);

			SaveChanges();

			//copy fields for original issue
			IQueryable<Field> fields = Fields.Where(x => x.ObjectID == orgIssue.ID && x.ObjectType == "Issue");

			foreach (Field field in fields)
			{
				Fields.Add(new Field { Value = field.Value, ObjectType = "Issue", ObjectID = issue.ID, FieldTypeID = field.FieldTypeID, UpdatedBy = CurrentResourceID });
			}

			SaveChanges();

			return issue;
		}

		public bool ExecuteTimerSteps()
		{
			string sql = @"
					select
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
							(vst.TimerLastRunDate is null or DATEADD(day, vst.settings.value('(/settings/TimerInterval)[1]', 'int'),vst.TimerLastRunDate) <= getutcdate() )
								and
							not exists(select * from workflow.itemsteptransition s_ist inner join workflow.itemstep s_isf on (s_isf.id = s_ist.fromitemstepid and s_isf.id = i_s.id) inner join workflow.itemstep s_isto on(s_isto.id = s_ist.toitemstepid and s_isto.itemid = i_s.itemid and s_isto.stepid = vst.toversionstepid))";

			List<dynamic> res = Query<dynamic>(sql).ToList();

			List<EventInfo> events = new List<EventInfo>();

			foreach (dynamic transition in res)
			{
				SystemObjects objectType = SystemObjects.Artifact;

				if (!Enum.TryParse<SystemObjects>(transition.Object, true, out objectType))
				{
					continue;
				}

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

				// update the timer step transition to be completed

				int.TryParse(transition.FromStepID.ToString(), out int fromTransitionId);
				int.TryParse(transition.ToStepID.ToString(), out int toTransitionId);
				WorkflowVersionStepTransition item = WorkflowVersionStepTransitions.Where(x => x.FromVersionStepID == fromTransitionId && x.ToVersionStepID == toTransitionId).FirstOrDefault();

				if (item == null)
				{
					throw new ArgumentNullException(nameof(item), "ERROR - CANNOT FIND THE WORKFLOW TRANSITION INSTANCE THAT WE NEED TO MARK AS COMPLETED");
				}

				item.TimerLastRunDate = DateTime.UtcNow;
				SaveChanges();
			}

			//add topic messages for the transitions
			if (events.Count > 0)
			{
				QueueSource.CreateTopicMessages(events);
			}

			return true;
		}

		public async Task<bool> ExecuteScheduledWorkflow(WorkflowEventRegistration registration)
		{
			Console.WriteLine($"DEBUG - CHECKING IF SCHEDULED WORKFLOW SHOULD RUN TYPE ID {registration.TypeID}");

			//check the last run date of this workflow against how often it runs
			if (registration.ChangeType != ChangeType.Schedule)
			{
				Console.WriteLine($"DEBUG - CURRENT REGISTRATION IS NOT OF CHANGE TYPE SCHEDULE NOT RUNNING.");

				return false;
			}

			if (string.IsNullOrEmpty(registration.Settings))
			{
				Console.WriteLine("DEBUG - CURRENT WORKFLOW DOESNT HAVE ANY SETTINGS CANNOT CONTINUE.");

				return false;
			}

			WorkflowEventRegistrationSettingsModel settings = WorkflowEventRegistrationSettingsModel.Parse(registration.Settings);

			int matchingItems = 0;
			List<string> items = new List<string>();

			if (settings.GetNextExecution(registration.LastExecuted) <= DateTime.UtcNow)
			{
				string sql = @"select 
										ad.ObjectID as ID,
										ad.DisplayValue
									from
										assetdetail ad
									where
										ad.object = @obj and ad.Typeid = @id";

				string issueSql = @"select 
											i.ID,
											t.Name + ' - ' + D.DisplayValue as DisplayValue
										from 
											Issue I
											inner join IssueType T on T.ID = I.IssueTypeID
											inner join AssetDetail D on D.Object = I.Object and D.ObjectID = I.ObjectID
										where 
											T.ID = @id";


				//evaluate objects that are part of this workflow
				switch ((registration.Object ?? "").ToUpper())
				{
					case "ARTIFACTTYPE":
						List<dynamic> artifacts = Query<dynamic>(sql, new { obj = "Artifact", id = registration.ObjectID }).ToList();
						foreach (dynamic artifact in artifacts)
						{
							if (await CreateWorkflowItem(registration.TypeID,
									new EventObjectInfo
									{
										Object = SystemObjects.Artifact,
										ObjectID = artifact.ID,
										ObjectType = SystemObjects.ArtifactType,
										ObjectTypeID = registration.ObjectID
									},
									registration,
									0))
							{
								matchingItems++;
								items.Add(artifact.DisplayValue);
							}
						}
						break;
					case "RULETYPE":
						List<dynamic> rules = Query<dynamic>(sql, new { obj = "Rule", id = registration.ObjectID }).ToList();
						foreach (dynamic rule in rules)
						{
							if (await CreateWorkflowItem(registration.TypeID,
									new EventObjectInfo
									{
										Object = SystemObjects.Rule,
										ObjectID = rule.ID,
										ObjectType = SystemObjects.RuleType,
										ObjectTypeID = registration.ObjectID
									},
									registration,
									0))
							{
								matchingItems++;
								items.Add(rule.DisplayValue);
							}
						}
						break;
					case "TAXONOMYTYPE":

						List<dynamic> taxonomies = Query<dynamic>(sql, new { obj = "Taxonomy", id = registration.ObjectID }).ToList();

						foreach (dynamic taxonomy in taxonomies)
						{
							if (await CreateWorkflowItem(registration.TypeID,
									new EventObjectInfo
									{
										Object = SystemObjects.Taxonomy,
										ObjectID = taxonomy.ID,
										ObjectType = SystemObjects.TaxonomyType,
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
					case "ISSUETYPE":
						List<dynamic> issues = Query<dynamic>(issueSql, new { id = registration.ObjectID }).ToList();

						foreach (dynamic issue in issues)
						{
							if (await CreateWorkflowItem(registration.TypeID,
									new EventObjectInfo
									{
										Object = SystemObjects.Issue,
										ObjectID = issue.ID,
										ObjectType = SystemObjects.IssueType,
										ObjectTypeID = registration.ObjectID,

									},
									registration,
									0))
							{
								matchingItems++;
								items.Add(issue.DisplayValue);
							}
						}
						break;
					default:
						//Nothing to do here.
						break;
				}

				//add item record for start and subsequent queue records
				registration.LastExecuted = DateTime.UtcNow;
				Entry(registration).State = EntityState.Modified;
				SaveChanges();

				//if the scheduled workflow needs an aggregate email send it
				if (settings.SendAggregateEmail && matchingItems > 0)
				{
					await SendAggregateWorkflowEmail(settings);
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

			WorkflowVersion version = WorkflowVersions
				.Include(i => i.Steps)
				.Where(i => i.TypeID == workflowTypeID && i.ID == registration.Type.PublishedVersionID)
				.OrderByDescending(i => i.Version)
				.FirstOrDefault();

			//if the object is an action, the requestor is the initiator of the action
			if (objectInfo.ObjectType == SystemObjects.IssueType)
			{
				Issue issue = await Issues.FirstOrDefaultAsync(i => i.ID == objectInfo.ObjectID);
				if (issue != null)
				{
					requestorId = issue.InitiatorID ?? requestorId;
				}
			}

			WorkflowItem item = new WorkflowItem
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
			WorkflowVersionStep firstVersionStep = version.Steps.Where(s => s.StepType == StepType.Start).FirstOrDefault();

			if (firstVersionStep == null)
			{
				Console.WriteLine("ERROR - WORKFLOW HAS NO START STEP.  DONE.");

				return true;
			}

			WorkflowItemStep firstItemStep = new WorkflowItemStep { CompletedBy = CurrentResourceID, CompletedOn = DateTime.UtcNow, StartedOn = DateTime.UtcNow, StartedBy = CurrentResourceID, Step = firstVersionStep, Fields = "<fields/>", Settings = "<settings/>", ItemID = item.ID };

			WorkflowItemSteps.Add(firstItemStep);
			SaveChanges();

			Console.WriteLine("DEBUG - PROCESSING START ITEM STEP.");

			List<WorkflowVersionStepTransition> transitions = WorkflowVersionStepTransitions
				.Where(i => i.FromVersionStepID == firstVersionStep.ID && i.TransitionType != TransitionType.Timer && i.State == State.Active)
				.ToList();

			//take any settings from the event registration and apply them in this start step
			if (!string.IsNullOrEmpty(registration.Settings))
			{
				Console.WriteLine("DEBUG - WORKFLOW HAS SETTINGS, STARTING TO SET THOSE.");

				//take the workflow settings right now this is only the visible column and apply these values if present.
				ProcessStartStepSettings(registration.Settings, objectInfo);
			}

			Console.WriteLine("DEBUG - STARTING WORKFLOW TRANSITIONS.");

			if (transitions.Count > 0)
			{
				await StartTransitions(transitions, item.ID, objectInfo);
			}

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
			}
		}

		private async Task StartTransitions(List<WorkflowVersionStepTransition> transitions, long itemID, EventObjectInfo objectInfo)
		{
			List<EventInfo> events = new List<EventInfo>();

			foreach (WorkflowVersionStepTransition transition in transitions)
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
			TelemetryClient client = new TelemetryClient();
			WorkflowVersionStepTransition transition = WorkflowVersionStepTransitions
				.Where(i => i.ID == versionStepTransitionID && i.State == State.Active).FirstOrDefault();

			if (transition == null)
			{
				throw new ArgumentNullException(nameof(transition), "ERROR - UNABLE TO LOCATE THE SPECIFIED WORKFLOW TRANSITION STEP");
			}

			bool transitionPassed = false;
			WorkflowItem item = null;
			Asset issueObject = null;

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
					item = WorkflowItems.FirstOrDefault(x => x.ID == itemID);

					if (item == null)
					{
						throw new ArgumentNullException(nameof(item), "ERROR UNABLE TO GET THE DETAILS FOR THIS WORKFLOW INSTANCE.");
					}

					if (item.Object == "Issue")
					{
						Issue issue = Issues.Find(item.ObjectID);
						if (issue != null)
						{
							issueObject = Assets.FirstOrDefault(a => a.Object == issue.Object && a.ObjectID == issue.ObjectID);
						}
					}

					//evaluate the condition then determine if we move to next step
					client.TrackEvent($"Condition Transition Evaluating.  Condition [{transition.Condition}], ItemID [{itemID}], VersionStepTransitionID [{versionStepTransitionID}]");
					transitionPassed = WorkflowRegistrationCriteriaProcessor.Evaluate(this, item.Object, item.ObjectID, transition.Condition, itemID, objectInfo.ChangedFieldIds, issueObject?.Object ?? "", issueObject?.ObjectID ?? -1);
					client.TrackEvent($"Condition Transition Evaluated.  Condition Result [{transitionPassed}], VersionStepTransitionID [{versionStepTransitionID}]");
					break;
				case TransitionType.Timer:
					//check if this timer transtion has a condition if so evaluate it
					Console.WriteLine("DEBUG - EVALUATING TIMER TRANSITION");
					transitionPassed = true;

					if (!string.IsNullOrEmpty(transition.Condition))
					{
						XElement root = XElement.Parse(transition.Condition);

						if (root != null && root.Element("Condition") != null)
						{
							item = WorkflowItems.FirstOrDefault(x => x.ID == itemID);

							if (item?.Object == "Issue")
							{
								Issue issue = Issues.Find(item.ObjectID);

								if (issue != null)
								{
									issueObject = Assets.FirstOrDefault(a => a.Object == issue.Object && a.ObjectID == issue.ObjectID);
								}
							}

							transitionPassed = WorkflowRegistrationCriteriaProcessor.Evaluate(this, item.Object, item.ObjectID, root.Element("Condition").Value, itemID, objectInfo.ChangedFieldIds, issueObject?.Object ?? "", issueObject?.ObjectID ?? -1);
						}
					}
					break;
				default:
					//Nothing to do here.
					break;
			}

			if (transitionPassed)
			{
				WorkflowItemStep fromItemStep = WorkflowItemSteps.Where(i => i.ItemID == itemID && i.StepID == transition.FromVersionStepID).FirstOrDefault();

				if (fromItemStep == null)
				{
					throw new ArgumentNullException(nameof(fromItemStep), "ERROR - CANNOT FIND ITEM FROM STEP");
				}

				long toItemStepID = 0;

				Console.WriteLine($"DEBUG ADDING WORKFLOW WORKFLOW.ITEMSTEP STEP ID [{transition.ToVersionStepID}] ITEM ID [{itemID}] ");

				WorkflowItemStep toItemStep = new WorkflowItemStep
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

				if (toItemStepID <= 0)
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

				EventInfo startEvent = new EventInfo
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
		public async Task ExecuteStep(long itemStepID, long itemID, EventInfo eventInfo)
		{
			bool isStepCompleted = false;
			WorkflowItemStep itemStep = getWorkflowItemStep(itemStepID);
			EventObjectInfo objectInfo = eventInfo.Object;

			//if the step is already done exit
			if (itemStep.CompletedOn.HasValue)
			{
				Console.WriteLine($"STEP WITH ID {itemStepID} HAS ALREADY COMPLETED NOT RERUNNING");

				return;
			}

			if (itemStep.Step == null)
			{
				Console.WriteLine($"STEP WITH ID {itemStepID} HAS NULL STEP REFERENCE CANNOT CONTINUE");

				return;
			}

			WorkflowItemStepSettingModel stepSettings = WorkflowItemStepSettingModel.ParseXml(itemStep.Step.Settings);

			//does the step need to wait for all transitions before running?
			if (stepSettings.WaitForAllTransitions)
			{
				//get count of the number of transitions to this step
				int expectedTransitionCount = WorkflowVersionStepTransitions.Where(x => x.ToVersionStepID == itemStep.StepID).Count();

				//get count of the completed transitions to this step                
				int completedTransitionsCount = Database.Connection.QueryFirstOrDefault<int>(@"select count(1) from
						workflow.itemsteptransition ist
						inner join workflow.itemstep iss on (ist.toitemstepid = iss.id)
					where 
						iss.stepid = @stepId and iss.itemid = @itemId", new { stepId = itemStep.StepID, itemId = itemStep.ItemID });

				if (expectedTransitionCount != completedTransitionsCount)
				{
					Console.WriteLine($"STEP WITH ID {itemStepID} HAS WAIT FOR ALL TRANSITIONS TO COMPLETE ENABLED NOT ALL HAVE COMPLETED expected:{expectedTransitionCount} actual completed:{completedTransitionsCount}");

					return;
				}
			}


			StepType stepType = itemStep.Step.StepType;

			Console.WriteLine($"Debug - Processing step of type {stepType}");

			if (stepType == StepType.Task)
			{
				Console.WriteLine($"Debug - Processing workflow task of type {itemStep.Step.ActivityType}");
				switch (itemStep.Step.ActivityType)
				{
					case WorkflowActivityType.EmailNotification:
						isStepCompleted = await SendWorkflowEmail(itemStep, eventInfo, stepSettings);
						break;
					case WorkflowActivityType.Form:
						// send form notification to owners
						await SendFormWorkflowEmail(itemStep, itemStepID, itemID, eventInfo, stepSettings);
						break;
					case WorkflowActivityType.StatusChange:
						// deprecated, just set to true and move on
						isStepCompleted = true;
						break;
					case WorkflowActivityType.Procedure:
						// execute proc with specified id
						ExecuteProc(objectInfo, stepSettings);
						isStepCompleted = true;
						break;
					case WorkflowActivityType.FieldChange:
						// change the specified field and mark the step complete
						await UpdateItemField(itemStep, objectInfo, stepSettings);
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
					case WorkflowActivityType.StateChange:
						ChangeItemState(itemStep);
						isStepCompleted = true;
						break;
					case WorkflowActivityType.HTTPRequest:
						await SendHttpRequestAsync(itemStep, objectInfo, stepSettings);
						isStepCompleted = true;
						break;
					case WorkflowActivityType.HTTPResponse:
						await ParseHttpResponseAsync(itemStep, stepSettings);
						isStepCompleted = true;
						break;
					default:
						isStepCompleted = true;
						break;
				}
			}
			else if (stepType == StepType.Finish || stepType == StepType.Terminate)
			{

				// if the task is a finish or terminate task we need to mark the workflow instance as completed and the task as completed
				isStepCompleted = true;

				WorkflowItem item = WorkflowItems.Where(x => x.ID == itemID).FirstOrDefault();

				if (item == null)
				{
					throw new ArgumentNullException(nameof(item), "ERROR - CANNOT FIND THE WORKFLOW INSTANCE THAT WE NEED TO MARK AS COMPLETED");
				}

				if (item.Object == SystemObjects.Issue.ToString() && item.ObjectID > 0)
				{
					Issue issue = Issues.FirstOrDefault(x => x.ID == item.ObjectID);
					if (issue != null)
					{
						if (issue.CompletedOn == null && issue.CompletedBy == null)
						{
							issue.CompletedOn = DateTime.UtcNow;
							issue.CompletedBy = CurrentResourceID;
						}
					}
				}

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

		private void ChangeItemState(WorkflowItemStep item)
		{
			Console.WriteLine("DEBUG - CHANGING ITEM STATE.");

			Console.WriteLine($"Executing - [workflow].[changeItemState] {item.Step.Version.TypeID}, {item.Step.ID}, {item.ItemID} ");

			Database.Connection.Execute("[workflow].[changeItemState] @id, @stepId, @itemId", new { id = item.Step.Version.TypeID, @stepId = item.Step.ID, @itemId = item.ItemID });
		}

		private async Task SendHttpRequestAsync(WorkflowItemStep item, EventObjectInfo info, WorkflowItemStepSettingModel settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings), $"ERROR - INVALID HTTP REQUEST SETTINGS SPECIFIED.");
			}

			WorkflowHttpRequestSettingsModel requestSettings = settings.HttpRequestSettings;

			if (string.IsNullOrEmpty(requestSettings.Url))
			{
				throw new ArgumentException($"ERROR - INVALID HTTP REQUEST URL SPECIFIED.");
			}

			string prefix = Community.GetPrimaryUrlPrefix();

			HttpRequestMessage request = new HttpRequestMessage();

			HttpClientHandler handler = new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			};

			using (HttpClient client = new HttpClient(handler))
			{

				switch (requestSettings.Method.ToUpper())
				{
					case "GET":
						request.Method = HttpMethod.Get;
						break;
					case "DELETE":
						request.Method = HttpMethod.Delete;
						break;
					case "POST":
						request.Method = HttpMethod.Post;
						break;
					case "PUT":
						request.Method = HttpMethod.Put;
						break;
					default:
						throw new ArgumentException($"ERROR - INVALID METHOD PASSED TO HTTP REQUEST.");
				}

				if (!string.IsNullOrEmpty(requestSettings.Body))
				{
					bool lookupfieldspassedbyvalue = requestSettings.LookupFieldsPassedByValue;
					string body = await ProcessMessageTokens(requestSettings.Body, info, prefix, item, false, true, lookupfieldspassedbyvalue);
					byte[] contentArray = Encoding.UTF8.GetBytes(body);
					request.Content = new ByteArrayContent(contentArray);
				}

				HttpResponseMessage response = null;
				try
				{
					if (requestSettings?.Headers?.Any() == true)
					{
						List<string> contentHeaderKeys = new List<string> { "content-type", "content-md5", "content-length", "content-encoding" };
						requestSettings.Headers.ForEach(async h =>
						{
							string value = await ProcessMessageTokens(h.Value, info, prefix, item, false);

							if (contentHeaderKeys.Contains(h.Key.ToLower()) && request.Content != null)
							{
								if (request.Content.Headers.Contains(h.Key))
								{
									request.Content.Headers.Remove(h.Key);
								}
								request.Content.Headers.TryAddWithoutValidation(h.Key, value);
							}
							else
							{
								if (request.Headers.Contains(h.Key))
								{
									request.Headers.Remove(h.Key);
								}
								request.Headers.TryAddWithoutValidation(h.Key, value);
							}
						});
					}

					string uri = await ProcessMessageTokens(requestSettings.Url, info, prefix, item, false);

					if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
					{
						throw new ArgumentException($"ERROR - INVALID HTTP REQUEST URL SPECIFIED.", nameof(uri));
					}

					request.RequestUri = new Uri(uri);
					requestSettings.FormattedUrl = new Uri(uri);
					client.Timeout = new TimeSpan(0, 0, requestSettings.Timeout);

					response = await client.SendAsync(request);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"ERROR - HTTP REQUEST TASK FAILED FOR ITEM [{item.ItemID}] STEP [{item.StepID}]\n" + ex.GetFullExceptionData());
				}

				await SaveHttpResponseResultsAsync(item, requestSettings, response);
			}
		}

		private async Task ParseHttpResponseAsync(WorkflowItemStep item, WorkflowItemStepSettingModel settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings), $"ERROR - INVALID HTTP RESPONSE SETTINGS SPECIFIED.");
			}

			WorkflowHttpResponseSettingsModel responseSettings = settings.HttpResponseSettings;

			if (string.IsNullOrEmpty(responseSettings.InputStepId))
			{
				throw new ArgumentException($"ERROR - INVALID HTTP RESPONSE STEP MISSING INPUT STEP ID.");
			}

			int stepId = -1;
			int.TryParse(responseSettings.InputStepId, out stepId);

			WorkflowItemStep requestStep = WorkflowItemSteps.FirstOrDefault(s => s.StepID == stepId && s.ItemID == item.ItemID);
			
			if (requestStep == null)
			{
				throw new ArgumentNullException(nameof(requestStep), $"ERROR - INVALID HTTP RESPONSE STEP MISSING REQUEST STEP.");
			}

			XElement requestStepFields = requestStep.FieldsDocument;
			string requestStepBody = requestStepFields?.Element("HTTPResponse")?.Element("Body")?.Value;

			if (string.IsNullOrEmpty(requestStepBody))
			{
				Console.WriteLine($"ERROR - INVALID HTTP RESPONSE BODY IS NULL OR EMPTY.");
			}

			JToken body = null;
			try
			{
				body = JToken.Parse(requestStepBody);
			}
			catch
			{
				Console.WriteLine($"ERROR - INVALID HTTP RESPONSE BODY IS NOT VALID JSON.");
			}

			if (!string.IsNullOrEmpty(item.Fields))
			{
				XElement root = XElement.Parse(item.Fields);
				XElement xOutputs = new XElement("Outputs");

				foreach (WorkflowHttpResponseOutput output in responseSettings.Outputs)
				{
					string value = "";
					XElement xOutput = new XElement("Output");

					if (body != null)
					{
						value = body.SelectToken(output?.Path ?? "", false)?.ToString() ?? "";
					}

					xOutput.Add(new XElement("Id", output.Id));
					xOutput.Add(new XElement("Value", value));

					xOutputs.Add(xOutput);
				}

				root.Add(xOutputs);
				item.Fields = root.ToString();
			}

			if (!string.IsNullOrEmpty(item.Settings))
			{
				XElement root = XElement.Parse(item.Settings);

				XElement xResponse = new XElement("HTTPResponse");
				xResponse.Add(new XElement("InputStepId", settings?.HttpResponseSettings?.InputStepId ?? ""));

				if (settings?.HttpResponseSettings?.Outputs?.Any() == true)
				{
					XElement xOutputs = new XElement("Outputs");
					foreach (WorkflowHttpResponseOutput output in settings.HttpResponseSettings?.Outputs)
					{
						XElement o = new XElement("Output");
						o.Add(new XElement("Id", output.Id));
						o.Add(new XElement("Name", output.Name));
						o.Add(new XElement("Path", output.Path));
						o.Add(new XElement("Type", output.Type));
						o.Add(new XElement("Format", output.Format));
						xOutputs.Add(o);
					}
					xResponse.Add(xOutputs);
					root.Add(xResponse);
					item.Settings = root.ToString();
				}
			}

			await SaveChangesAsync();
		}

		private void DeleteItemWorkflowActivity(EventObjectInfo objectInfo)
		{
			Console.WriteLine("DEBUG - DELETING ITEM.");

			if (objectInfo.Object == SystemObjects.Intersect)
			{
				DeleteRelationship(objectInfo.ObjectID);
			}
			else
			{
				Delete(objectInfo.Object, objectInfo.ObjectID);
			}
		}

		private void UpdateItemRelationship(WorkflowItemStep itemStep, EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
		{
			if (!settings.RelationshipUpdateSettings.Any())
			{
				throw new ArgumentException($"ERROR - INVALID RELATIONSHIP UPDATE SETTINGS SPECIFIED.");
			}

			foreach (WorkflowRelationshipUpdateSettings item in settings.RelationshipUpdateSettings)
			{
				if (string.IsNullOrEmpty(item.FormField) || item.FormStepID <= 0)
				{
					throw new ArgumentException($"ERROR - INVALID FORM FIELD OR FORM STEP ID SPECIFIED FOR RELATIONSHIP UPDATE STEP.  FORM FIELD IS : [{item.FormField}] FORM STEP IS : [{item.FormStepID}]");
				}

				WorkflowVersionStep verStep = WorkflowVersionSteps.Where(x => x.ID == item.FormStepID).FirstOrDefault();

				if (verStep == null)
				{
					throw new ArgumentNullException(nameof(verStep), $"ERROR - FORM STEP TO USE AS THE SOURCE OF THE RELATIONSHIP IS INVALID AND CANNOT BE LOADED");
				}

				//load the intersect from the form fields                
				WorkflowFormFormModel formFields = WorkflowFormFormModel.ParseXml(XElement.Parse(verStep.Fields).Element("form"));

				WorkflowFormFieldModel field = formFields.Fields.Where(x => x.ID == item.FormField).FirstOrDefault();

				if (field == null)
				{
					throw new ArgumentNullException(nameof(field), $"ERROR - CANNOT FIND FORM FIELD TO USE AS INPUT FOR RELATIONSHIP.");
				}

				if (!int.TryParse(field.IntersectTypeID, out int intersectTypeId))
				{
					throw new ArgumentException("ERROR - CANNOT PARSE THE INTERSECTTYPEID VALUE AS AN INTEGER");
				}

				//get intersect type info
				IntersectType intersectType = IntersectTypes.Where(x => x.ID == intersectTypeId).FirstOrDefault();

				if (intersectType == null)
				{
					throw new ArgumentNullException(nameof(intersectType), $"ERROR - INVALID INTERSECT TYPE ID SPECIFIED.  PLEASE CHECK THE SETTINGS ASSOCIATED WITH THE RELATIONSHIP UPDATE ACTION OF THE CURRENT WORKFLOW. INTERSECT TYPE ID IS [{intersectTypeId}]");
				}

				EventObjectInfo assetInfo;

				//get underlying asset if this is an action
				if (objectInfo.ObjectType == SystemObjects.IssueType)
				{
					Issue issue = Issues.FirstOrDefault(i => i.ID == objectInfo.ObjectID);

					if (issue == null)
					{
						throw new ArgumentNullException(nameof(issue), $"ERROR - ASSET FOR ACTION ID [{objectInfo.ObjectID}] NOT FOUND");
					}

					assetInfo = new EventObjectInfo
					{
						Object = (SystemObjects)Enum.Parse(typeof(SystemObjects), issue.Object),
						ObjectID = issue.ObjectID,
						ObjectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), issue.ObjectType),
						ObjectTypeID = issue.ObjectTypeID
					};
				}
				else
				{
					assetInfo = new EventObjectInfo
					{
						Object = objectInfo.Object,
						ObjectID = objectInfo.ObjectID,
						ObjectType = objectInfo.ObjectType,
						ObjectTypeID = objectInfo.ObjectTypeID
					};
				}

				bool isSubject = intersectType.SubjectID == assetInfo.ObjectTypeID && intersectType.Subject == assetInfo.ObjectType.ToString();

				if (item.ClearValue)
				{
					//delete intersects with the given intersect type id for the current object
					DeleteIntersects(assetInfo.Object, assetInfo.ObjectID, intersectTypeId, isSubject);
				}
				else
				{
					string val = GetFieldValueIntersectFromFormResponse(item, itemStep.ItemID);

					bool supportsJustOne = isSubject ? intersectType.ObjectCardinality == Cardinality.One : intersectType.SubjectCardinality == Cardinality.One;

					if (!item.AppendValue || supportsJustOne)
					{
						DeleteIntersects(assetInfo.Object, assetInfo.ObjectID, intersectTypeId, isSubject);
					}

					//split the value on , 
					string[] rels = val.Split(',');

					// if it just supports one item just use the first selected.
					if (supportsJustOne)
					{
						rels = new string[] { rels.First() };
					}

					List<DatabaseBulkRelationshipResult> graphEvents = new List<DatabaseBulkRelationshipResult>();

					foreach (string rel in rels)
					{
						// split by | for type id
						// add item
						if (!string.IsNullOrEmpty(rel))
						{
							string[] parts = rel.Split('|');

							Intersect intersect = new Intersect
							{
								IntersectTypeID = intersectType.ID
							};

							if (isSubject)
							{
								intersect.Subject = assetInfo.Object.ToString();
								intersect.SubjectID = assetInfo.ObjectID;
								intersect.Object = (parts[0] ?? "").Replace("Type", "");
								intersect.ObjectID = int.Parse(parts[1]);
							}
							else
							{
								intersect.Object = assetInfo.Object.ToString();
								intersect.ObjectID = assetInfo.ObjectID;
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

								graphEvents.Add(new DatabaseBulkRelationshipResult
								{
									Object = "Intersect",
									uid = intersect.uid,
									Success = true
								});
							}
						}
					}

					if (graphEvents.Any())
					{
						SendAssetGraphEvents(graphEvents);
					}
				}
			}
		}

		private void DeleteIntersects(SystemObjects @object, int objectID, int intersectTypeId, bool isSubject)
		{
			string sql;
			List<DatabaseBulkRelationshipResult> graphEvents;

			if (isSubject)
			{
				sql = "delete from [intersect] output deleted.uid into #deletedIntersects where subject = @obj and subjectid = @objectid and intersecttypeid = @intersectTypeId";
			}
			else
			{
				sql = "delete from [intersect] output deleted.uid into #deletedIntersects where [object] = @obj and objectid = @objectid and intersecttypeid = @intersectTypeId";
			}

			graphEvents = Database.Connection.Query<DatabaseBulkRelationshipResult>($@"
				drop table if exists #deletedIntersects;
				create table #deletedIntersects (uid uniqueidentifier);
				{sql}
				select uid, 'Intersect' as [Object], cast(1 as bit) as Success from #deletedIntersects"
				, new { obj = @object.ToString(), objectid = objectID, intersectTypeId })
				.ToList();

			if (graphEvents.Any())
			{
				SendAssetGraphEvents(graphEvents);
			}
		}

		private async Task UpdateField(int objectId, string objectType, FieldType fieldType, WorkflowFieldUpdateSettings item, string val, bool isAssetEdited = false, Asset asset = null)
		{
			//check if the field exists
			Field field = Fields.Where(x => x.ObjectID == objectId && x.ObjectType == objectType && x.FieldTypeID == fieldType.ID).FirstOrDefault();

			//validate list field value
			if (fieldType.Type == DataType.Lookup.ToString() && !string.IsNullOrEmpty(val))
			{
				List<int> lookupValues = val.Split(',').Select(x => int.Parse(x)).ToList();
				List<int> value = (await QueryAsync<int>(@"select value
				  from[dbo].[FieldLookupValue]
				  where LookupObjectType = @obj and LookupObjectID = @objId and FieldTypeID = @f and [Value] in @lookupValues",

				new { obj = fieldType.LookupObjectType, objId = fieldType.LookupObjectID, f = fieldType.ID, lookupValues })
					).ToList();

				if (value.Count == 0)
				{
					//do not update list field when it is invalid
					Console.WriteLine($"Warning - UpdateField : Invalid Lookup value detected. Update field skipped");

					return;
				}

				if (value.Count != lookupValues.Count)
				{
					val = string.Join(",", lookupValues);
					Console.WriteLine($"Warning - UpdateField : Some invalid lookup values detected. Field value updated partially");
				}
			}

			//use SQL here instead of EF to avoid triggering further workflows
			if (field == null && !string.IsNullOrEmpty(val))
			{
				await Database.Connection.ExecuteAsync("insert into [Field] (AssetID, FieldTypeID, ObjectID, ObjectType, [Value], UpdatedBy) values (@assetID, @fieldTypeID, @objectId, @objectType, @value, @updatedBy)"
					, new
					{
						value = val,
						fieldTypeID = fieldType.ID,
						assetID = isAssetEdited ? asset.ID : (long?)null,
						objectId,
						objectType = objectType.ToString(),
						updatedBy = CurrentResourceID
					});
			}
			else if (field != null)
			{
				string updateValue = val;

				if (item.AppendValue)
				{
					IEnumerable<string> oldValues = field.Value?.Split(',').Where(s => !string.IsNullOrEmpty(s.Trim())).Select(x => x.Trim()) ?? new string[0];
					IEnumerable<string> newValues = val?.Split(',').Where(s => !string.IsNullOrEmpty(s.Trim())).Select(x => x.Trim()) ?? new string[0];
					newValues = oldValues.Union(newValues).Distinct().OrderBy(x => x);
					updateValue = string.Join(",", newValues);

				}

				//remove the field from db if field value is null or empty 
				if (string.IsNullOrEmpty(updateValue))
				{
					await Database.Connection.ExecuteAsync("delete from Field where FieldTypeID = @fieldTypeID and ObjectType = @objectType and ObjectID = @objectId"
					, new
					{
						fieldTypeID = field.FieldTypeID,
						objectType = field.ObjectType,
						objectId = field.ObjectID
					});
				}
				else //update
				{
					await Database.Connection.ExecuteAsync("update Field set[Value] = @value, UpdatedOn = getutcdate(), UpdatedBy = @updatedBy where FieldTypeID = @fieldTypeID and ObjectType = @objectType and ObjectID = @objectId"
					, new
					{
						value = updateValue,
						updatedBy = CurrentResourceID,
						fieldTypeID = field.FieldTypeID,
						objectType = field.ObjectType,
						objectId = field.ObjectID
					});
				}
			}
		}

		private async Task UpdateItemField(WorkflowItemStep itemStep, EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
		{
			if (!settings.FieldUpdateSettings.Any())
			{
				return;
			}

			Issue issue = Issues.FirstOrDefault(x => x.ID == objectInfo.ObjectID);
			Asset asset = null;
			AssetType assetType = null;
			bool isAssetEdited = false;

			foreach (WorkflowFieldUpdateSettings item in settings.FieldUpdateSettings)
			{
				// get field type info
				FieldType fieldType = FieldTypes.Where(x => x.ID == item.FieldID).FirstOrDefault();
				int objectId = objectInfo.ObjectID;
				string objectType = objectInfo.Object.ToString();

				if (objectInfo.Object.ToString() == "Issue" && !string.IsNullOrEmpty(item.ObjectType) && item.ObjectType != "Issue")
				{
					objectType = issue.Object;
					objectId = issue.ObjectID;
					asset = Assets.Where(x => x.Object == issue.Object && x.ObjectID == issue.ObjectID).FirstOrDefault();
					assetType = AssetTypes.FirstOrDefault(a => a.Object == issue.ObjectType && a.ObjectID == issue.ObjectTypeID);
					ObjectContext.ObjectStateManager.ChangeObjectState(asset, EntityState.Modified);
					isAssetEdited = true;
				}
				else
				{
					asset = Assets.Where(x => x.Object == objectInfo.Object.ToString() && x.ObjectID == objectInfo.ObjectID).FirstOrDefault();
					assetType = AssetTypes.FirstOrDefault(a => a.Object == objectInfo.ObjectType.ToString() && a.ObjectID == objectInfo.ObjectTypeID);
				}

				if (fieldType == null)
				{
					throw new ArgumentNullException(nameof(fieldType), $"ERROR - INVALID FIELD TYPE ID SPECIFIED FOR UPDATE FIELD WORKFLOW TASK. FIELD ID[ {item.FieldID} ]");
				}

				if (item.ClearValue)
				{
					//delete the value
					string sql = "delete field where objectid = @id and objecttype = @objectType and fieldtypeid = @fieldTypeId";

					await Database.Connection.ExecuteAsync(sql, new { id = objectId, objectType = objectType, fieldTypeId = item.FieldID });

				}
				else if (item.CurrentDate)
				{
					string val = DateTime.UtcNow.Date.ToShortDateString();
					await UpdateField(objectId, objectType, fieldType, item, val);
				}
				else if (!item.IsActionForm && !item.UseFormValue && !item.UseOutputValue)
				{
					string val = item.Value;
					await UpdateField(objectId, objectType, fieldType, item, val);
				}
				//if the value is a form value get it
				else if (!item.IsActionForm && item.UseFormValue && !string.IsNullOrEmpty(item.FormField) && item.FormStepID > 0)
				{

					foreach (string newValue in GetFieldValueFromFormResponse(item, itemStep.ItemID))
					{
						string val = newValue;

						if (DateTime.TryParse(val, out DateTime tempDate))
						{
							val = tempDate.Date.ToShortDateString();
						}
						await UpdateField(objectId, objectType, fieldType, item, val);
					}
				}
				//Get the value from action form (Issue)
				else if (item.IsActionForm)
				{
					string val = "";
					string[] fieldData = item.FormField.Split('|');
					
					if (fieldData.Count() == 2)
					{
						int fieldTypeId = int.Parse(fieldData[1]);
						Field actionField = Fields.FirstOrDefault(x => x.ObjectID == objectInfo.ObjectID && x.ObjectType == "Issue" && x.FieldTypeID == fieldTypeId);
						
						if (actionField != null)
						{
							FieldType actionFieldType = FieldTypes.FirstOrDefault(x => x.Object == "IssueType" && x.ID == actionField.FieldTypeID);
							
							if (actionFieldType.Type == "Lookup" || actionFieldType.Type == "Link")
							{
								val = actionField?.Value;
							}
							else
							{
								val = actionField?.FormattedValue;
							}

							if (DateTime.TryParse(val, out DateTime tempDate) && (actionFieldType.Type == "Date" || actionFieldType.Type == "DateTime"))
							{
								val = tempDate.Date.ToShortDateString();
							}

						}
					}

					await UpdateField(objectId, objectType, fieldType, item, val, isAssetEdited, asset);
				}
				else if (item.UseOutputValue)
				{
					string val = GetOutputFieldValue(item.FormStepID, itemStep.ItemID, item.FormField);
					await UpdateField(objectId, objectType, fieldType, item, val, isAssetEdited, asset);
				}
			}

			if (asset != null)
			{
				CreateWorkflowItemFieldUpdateExecution(assetType, asset); // Send scoring updates
			}

			await SaveChangesAsync();

			if (asset != null)
			{
				await Database.Connection.ExecuteAsync("update asset set updatedby = @updatedBy, updatedOn = GETUTCDATE() where id = @id", new { updatedBy = CurrentResourceID, id = asset.ID });
			}

			//update asset table to trigger audit                    
			await Database.Connection.ExecuteAsync(
					 "exec [utility].[AddAuditEntry]  @ParentObject, @ParentObjectID, @ResourceID, @date, @op, @Object, @ObjectID",
					 new
					 {
						 Object = (asset != null) ? asset.Object : objectInfo.Object.ToString(),
						 ObjectID = (asset != null) ? asset.ObjectID : objectInfo.ObjectID,
						 ParentObject = objectInfo.Object.ToString(),
						 date = DateTime.UtcNow,
						 ParentObjectID = (asset != null) ? asset.ObjectID : objectInfo.ObjectID,
						 ResourceID = CurrentResourceID,
						 op = "Updated"
					 });
		}

		private string GetFieldValueIntersectFromFormResponse(WorkflowRelationshipUpdateSettings item, long itemId)
		{
			IQueryable<WorkflowItemStep> formResponses = WorkflowItemSteps.Where(x => x.ItemID == itemId && x.StepID == item.FormStepID && x.Step.ActivityType == WorkflowActivityType.Form);

			WorkflowItemStep firstResponse = formResponses.FirstOrDefault();

			if (firstResponse == null)
			{
				return "";
			}

			XElement xml = XElement.Parse(firstResponse.Fields);

			foreach (XElement form in xml.Elements("form"))
			{
				foreach (XElement field in form.Elements("field"))
				{
					if ((string)field.Attribute("id") == item.FormField)
					{
						return (string)field.Attribute("value");
					}
				}
			}

			return "";
		}

		private IEnumerable<string> GetFieldValueFromFormResponse(WorkflowFieldUpdateSettings item, long itemId)
		{
			IQueryable<WorkflowItemStep> formResponses = WorkflowItemSteps.Where(x => x.ItemID == itemId && x.StepID == item.FormStepID && x.Step.ActivityType == WorkflowActivityType.Form);

			WorkflowItemStep firstResponse = formResponses.FirstOrDefault();

			XElement root = XElement.Parse(firstResponse.Fields);

			IEnumerable<XElement> fields =
			from el in root.Elements("form").Elements("field")
			where (string)el.Attribute("id") == item.FormField
			select el;

			foreach (XElement el in fields)
			{
				yield return (string)el.Attribute("value");
			}
		}

		public string GetOutputFieldValue(int stepId, long itemId, string fieldId)
		{
			WorkflowItemStep step = WorkflowItemSteps.FirstOrDefault(s => s.StepID == stepId && s.ItemID == itemId);

			if (step != null)
			{
				XElement stepFields = step.FieldsDocument;

				if (stepFields != null)
				{
					IEnumerable<XElement> outputs = stepFields.Element("Outputs").Elements("Output");

					if (outputs != null)
					{
						foreach (XElement output in outputs)
						{
							if (output.Element("Id")?.Value == fieldId)
							{
								return output.Element("Value")?.Value ?? "";
							}
						}
					}
				}
			}

			return "";
		}

		private void ExecuteProc(EventObjectInfo objectInfo, WorkflowItemStepSettingModel settings)
		{
			if (settings.StoredProcedureID <= 0)
			{
				Console.WriteLine($"DEBUG : STORED PROC STEP DOESNT HAVE A VALID PROCEDURE ID.");

				return;
			}

			WorkflowTaskProcedure procInfo = WorkflowTaskProcedures.Where(x => x.ID == settings.StoredProcedureID).FirstOrDefault();


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

		private void SaveItemAssignments(IEnumerable<GlobalReportingResource> users, long itemId, long itemStepId)
		{
			foreach (GlobalReportingResource user in users)
			{
				WorkflowItemAssignment assignment = new WorkflowItemAssignment
				{
					CreatedBy = 0,
					CreatedOn = DateTime.UtcNow,
					ItemStepID = itemStepId,
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
			IQueryable<WorkflowItemAssignment> itemAssignments = WorkflowItemAssignments.Where(x => x.ItemID == itemID);

			foreach (WorkflowItemAssignment assignment in itemAssignments)
			{
				WorkflowItemAssignments.Remove(assignment);
			}

			SaveChanges();
		}

		public void CompleteItemStepAssignments(long itemStepID)
		{
			IQueryable<WorkflowItemAssignment> itemAssignments = WorkflowItemAssignments.Where(x => x.ItemStepID == itemStepID);

			foreach (WorkflowItemAssignment assignment in itemAssignments)
			{
				WorkflowItemAssignments.Remove(assignment);
			}

			SaveChanges();
		}

		public void RequestObjectCertification(SystemObjects @object, int objectId, SystemObjects objectType, int objectTypeId)
		{
			List<EventInfo> events = new List<EventInfo>
			{
				new EventInfo
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
				}
			};

			QueueSource.CreateTopicMessages(events);
		}

		public async Task<int> MarkStepAsCompleteAndContinue(WorkflowItemStep itemStep, long itemID, EventObjectInfo objectInfo)
		{
			// mark step as completed
			itemStep.CompletedOn = DateTime.UtcNow;
			itemStep.CompletedBy = CurrentResourceID;
			SaveChanges();

			// get the transitions for this step and add events
			List<WorkflowVersionStepTransition> transitions = WorkflowVersionStepTransitions
			.Where(i => i.FromVersionStepID == itemStep.StepID && i.TransitionType != TransitionType.Timer && i.State == State.Active)
			.ToList();

			if (transitions.Count > 0)
			{
				await StartTransitions(transitions, itemID, objectInfo);
			}

			return transitions.Count;
		}

		/// <summary>
		/// Reassign one or more form steps to a new user
		/// </summary>
		/// <param name="itemSteps">Form steps to reassign</param>
		/// <param name="resource">The resource to assign the forms to</param>
		/// <param name="originalResourceId">The resource Id of the original assignee on the form</param>
		/// <param name="sendFormEmails">Whether or not to resend form emails. If the step doesn't have form emails configured this setting is ignored</param>
		/// <returns></returns>
		public async Task BulkWorkflowFormReassign(List<WorkflowItemStep> itemSteps, GlobalReportingResource resource, int originalResourceId, bool sendFormEmails = true, bool clearAssignments = false)
		{
			foreach (WorkflowItemStep itemStep in itemSteps)
			{
				if (itemStep.Step.ActivityType != WorkflowActivityType.Form)
				{
					continue;
				}

				WorkflowItemStepSettingModel stepSettings = WorkflowItemStepSettingModel.ParseXml(itemStep.Step.Settings);

				XElement fieldElement = XElement.Parse(itemStep.Fields);
				XElement reassigned = new XElement("Reassigned");
				string objectType = "";
				int objectId = 0;
				bool isResourceReassignment = true;

				if (stepSettings.ResponsibilityTypeID > 0 || (stepSettings.RecipientGroup != null || stepSettings.RecipientGroup != Guid.Empty))
				{
					if (stepSettings.RecipientType == EmailTaskRecipientType.Responsibility)
					{
						isResourceReassignment = false;
						objectType = SystemObjects.ResponsibilityType.ToString();
						objectId = stepSettings.ResponsibilityTypeID;
					}
					else if (stepSettings.RecipientType == EmailTaskRecipientType.Group)
					{
						Asset group = Assets.Where(x => x.uid == stepSettings.RecipientGroup).FirstOrDefault();
						
						if (group != null)
						{
							isResourceReassignment = false;
							objectType = SystemObjects.Group.ToString();
							objectId = group.ObjectID;
						}
					}
					else if (stepSettings.RecipientType == EmailTaskRecipientType.SpecificUser)
					{
						isResourceReassignment = false;
						objectType = "Specific Users";
						objectId = -1;
					}
					DateTime date = DateTime.MinValue;
					string type = "";
					foreach (XElement elem in fieldElement.Elements("Reassigned"))
					{
						DateTime reassignTime = DateTime.Parse(elem.Attribute("reassignOn").Value);
						
						if (date < reassignTime)
						{
							date = reassignTime;
							type = elem.Attribute("reassignType").Value;
							isResourceReassignment = type == "Resource";
						}
					}
				}

				if (isResourceReassignment)
				{
					reassigned.Add(new XAttribute("toResourceId", resource.ResourceID.ToString()));
					reassigned.Add(new XAttribute("fromResourceId", originalResourceId.ToString()));
				}
				else
				{
					reassigned.Add(new XAttribute("toResourceId", resource.ResourceID.ToString()));
					reassigned.Add(new XAttribute("objectId", objectId));
					reassigned.Add(new XAttribute("objectType", objectType));
				}

				reassigned.Add(new XAttribute("reassignType", "Resource"));
				reassigned.Add(new XAttribute("byResourceId", CurrentResourceID.ToString()));
				reassigned.Add(new XAttribute("reassignOn", DateTime.UtcNow));


				fieldElement.Add(reassigned);
				itemStep.Fields = fieldElement.ToString();
				itemStep.StartedOn = DateTime.UtcNow;

				List<WorkflowItemAssignment> currentAssignments;

				if (clearAssignments)
				{
					currentAssignments = WorkflowItemAssignments.Where(x => x.ItemStepID == itemStep.ID && x.ResourceObject == "Resource").ToList();
					WorkflowItemStepDetail.FieldsModel itemFields = (WorkflowItemStepDetail.FieldsModel)new XmlSerializer(typeof(WorkflowItemStepDetail.FieldsModel)).Deserialize(new StringReader(itemStep.Fields));
					itemFields.NumberOfResponses = 1;
					itemFields.TotalResources = 1;
					using (StringWriter sr = new StringWriter())
					{
						XmlSerializer serializer = new XmlSerializer(typeof(WorkflowItemStepDetail.FieldsModel));
						serializer.Serialize(sr, itemFields);
						itemStep.Fields = sr.ToString();
					}
				}
				else
				{
					//only remove assignments for current resource and target resource to avoid duplicates 
					List<int> removeResourceIds = new List<int> { originalResourceId, resource.ResourceID };

					currentAssignments = WorkflowItemAssignments.Where(x => x.ItemStepID == itemStep.ID && x.ResourceObject == "Resource" && !removeResourceIds.Contains(x.ResourceObjectID)).ToList();
					List<WorkflowItemAssignment> resourceAssignments = WorkflowItemAssignments.Where(x => x.ItemStepID == itemStep.ID && x.ResourceObject == "Resource" && removeResourceIds.Contains(x.ResourceObjectID)).ToList();
					
					if (resourceAssignments.Any())
					{
						WorkflowItemAssignments.RemoveRange(resourceAssignments);
					}
				}

				if (currentAssignments.Any() && clearAssignments)
				{
					WorkflowItemAssignments.RemoveRange(currentAssignments);
				}

				if (sendFormEmails && stepSettings.FormShouldSendEmail)
				{
					string obj = itemStep.Item.Object;
					int objId = itemStep.Item.ObjectID;

					ObjectDetail objectDetail = GetObjectDetail(obj, objId);

					EventObjectInfo objEventInfo = new EventObjectInfo
					{
						ObjectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), objectDetail.Type),
						ObjectTypeID = objectDetail.TypeID,
						Object = (SystemObjects)Enum.Parse(typeof(SystemObjects), obj),
						ObjectID = objId,
					};
					EventInfo eventInfo = new EventInfo
					{
						Action = ChangeType.Update,
						Object = objEventInfo
					};

					XElement settings = itemStep.SettingsDocument;
					XElement emails = settings.Element("emails");

					if (emails != null)
					{
						emails.Remove();
					}

					itemStep.Settings = settings.ToString();
					await SaveChangesAsync();

					//resend email to the reassigned user
					stepSettings.SpecificUser = resource.Email;
					stepSettings.RecipientType = EmailTaskRecipientType.SpecificUser;
					await SendFormWorkflowEmail(itemStep, itemStep.ID, itemStep.ItemID, eventInfo, stepSettings);
				}
				else
				{
					WorkflowItemAssignment assignment = new WorkflowItemAssignment
					{
						ItemStepID = itemStep.ID,
						ItemID = itemStep.ItemID,
						CreatedBy = CurrentResourceID,
						CreatedOn = DateTime.UtcNow,
						ResourceObject = "Resource",
						ResourceObjectID = resource.ResourceID,
						UpdatedBy = CurrentResourceID,
						UpdatedOn = DateTime.UtcNow
					};

					WorkflowItemAssignments.Add(assignment);
				}
			}

			await SaveChangesAsync();
		}

		public async Task<bool> SendFormWorkflowEmail(WorkflowItemStep item, long itemStepID, long itemId, EventInfo eventInfo, WorkflowItemStepSettingModel settings)
		{
			List<string> emailedUsers = new List<string>();
			List<GlobalReportingResource> users = new List<GlobalReportingResource>();
			int typeId = item?.Step?.Version?.TypeID ?? 0;
			string typeName = item?.Step?.Version?.Type?.Name ?? "";
			EventObjectInfo objectInfo = eventInfo.Object;
			//based on the step settings get the users

			List<SettingInfo> companySettings = GetSettings();
			string fromName = companySettings.First(s => s.ID == Setting.WorkflowFromName).Value;
			string fromEmail = companySettings.First(s => s.ID == Setting.WorkflowFromEmail).Value;

			if (settings.RecipientType == EmailTaskRecipientType.Initiator)
			{
				if (item.Item.StartedBy <= 0)
				{
					Console.WriteLine("ERROR CANNOT DETERMINE WHO TO ASSIGN FORM STEP TO.");

					return true;
				}

				GlobalReportingResource res = GlobalReportingResources.Where(x => x.ResourceID == item.Item.StartedBy).FirstOrDefault();

				if (res == null)
				{
					Console.WriteLine("ERROR CANNOT FIND THE RESOURCE WHO STARTED THE WORKFLOW TO ASSIGN FORM TO.");

					return true;
				}

				users.Add(res);

				Console.WriteLine($"DEBUG : FORM STEP IS ASSIGNED TO [{res.Email}].");
			}
			else if (settings.RecipientType == EmailTaskRecipientType.Responsibility || settings.RecipientType == EmailTaskRecipientType.None)
			{
				if (settings.RecipientType == EmailTaskRecipientType.Responsibility)
				{
					if (await ShouldWaitForResponsibilityRuleToRun(item, settings, itemStepID, eventInfo))
					{
						return false;
					}
				}

				users = GetWorkflowUsersBasedOnResponsibility(typeId, item.Step.ID, item.ItemID).ToList();
			}
			else if (settings.RecipientType == EmailTaskRecipientType.SpecificUser)
			{
				if (string.IsNullOrEmpty(settings.SpecificUser))
				{
					Console.Write("ERROR - NO USER SPECIFIED FOR THE SPECIFIC USER FORM TASK.");

					return true;
				}

				Console.WriteLine($"DEBUG : FORM STEP IS ASSIGNED TO [{settings.SpecificUser}].");

				foreach (string email in settings.SpecificUser.Split(';'))
				{
					GlobalReportingResource res = GlobalReportingResources.Where(x => string.Compare(x.Email, email.Trim(), true) == 0).FirstOrDefault();
					if (res == null)
					{
						Console.WriteLine("FORM EMAIL SPECIFIC USER SET HOWEVER THE USER EMAIL IS NOT A VALID D3S EMAIL ACCOUNT.  WONT BE ABLE TO ASSIGN FORM TO USER..");

						continue;
					}

					users.Add(res);
				}
			}
			else if (settings.RecipientType == EmailTaskRecipientType.Group)
			{
				if (settings.RecipientGroup == Guid.Empty)
				{
					Console.Write("ERROR - NO GROUP SPECIFIED FOR THE GROUP FORM TASK.");
					
					return true;
				}

				int recipientGroup = Query<int>(@"select ObjectID from asset where [Object] = 'Group' and uid = @Uid", new { Uid = settings.RecipientGroup }).FirstOrDefault();
				if (recipientGroup <= 0)
				{
					Console.Write("ERROR - INVALID GROUP FOR THE GROUP FORM TASK.");
					
					return true;
				}

				users = GetWorkflowUsersBasedOnGroup(recipientGroup).ToList();
			}

			string prefix = Community.GetPrimaryUrlPrefix();
			string url = $"https://{prefix}.data3sixty.com/workflow/form/{typeId}/{itemStepID}/{itemId}";

			string initiatedBy = "(unknown)";

			if (item.StartedBy > 0)
			{
				GlobalReportingResource res = GlobalReportingResources.Where(x => x.ResourceID == item.StartedBy).FirstOrDefault();

				if (res != null)
				{
					initiatedBy = res.FullName;
				}
			}

			//update the xml for the number of users sent the form
			XElement xml = XElement.Parse(item.Fields);

			if (!xml.Attributes("TotalResources").Any())
			{
				xml.Add(new XAttribute("TotalResources", users.Count()));
			}

			item.Fields = xml.ToString();
			await SaveChangesAsync();

			if (settings.FormShouldSendEmail)
			{
				ObjectDetail obj = GetObjectDetail(objectInfo.Object.ToString(), objectInfo.ObjectID);

				string itemName = (obj == null) ? "(unknown)" : obj.Name;
				string emailSubject = "";

				if (!string.IsNullOrEmpty(settings.SubjectTemplate))
				{
					emailSubject = await ProcessMessageTokens(settings.SubjectTemplate, objectInfo, prefix, item, false);
				}
				else
				{
					emailSubject = $"Data3Sixty - Workflow [{typeName}] - Form";
				}

				string emailBody = $"<p>The Data3Sixty workflow <b>{typeName}</b> has generated a form that you need to complete for the item <b>{itemName}</b>.  This workflow was initiated by {initiatedBy}.  Please complete the form at {url}</p>";

				string customBody = await ProcessMessageTokens(settings.BodyTemplate, objectInfo, prefix, item);

				if (!string.IsNullOrEmpty(customBody))
				{
					emailBody = $"{customBody} <br>Please complete the form at {url}";
				}

				if (settings.ShouldIncludeFormResponses)
				{
					emailBody += GenerateFormResponsesEmailContent(item.ItemID);
				}

				string emailBase = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\">{emailBody}</body></html>";

				foreach (GlobalReportingResource user in users)
				{
					Console.WriteLine($"DEBUG : FORM STEP EMAIL IS EMAILING [{user.Email}].");

					emailedUsers.Add(user.Email);

					try
					{
						await Mail.SendMessage(emailSubject, user.Email, user.FirstName + " " + user.LastName, emailBase, true, fromEmail, fromName);
					}
					catch (Exception e)
					{
						//error sending email
						TelemetryClient client = new TelemetryClient();
						client.TrackException(e, new Dictionary<string, string> { { "CompanyID", CurrentCompanyID.ToString() } });
					}
				}

				SaveItemStepEmailedUsers(item, emailedUsers);
			}

			SaveItemAssignments(users, itemId, itemStepID);

			return true;
		}

		private async Task SendAggregateWorkflowEmail(WorkflowEventRegistrationSettingsModel settings)
		{
			settings.EmailMessageTemplate = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\">{settings.EmailMessageTemplate}";

			settings.EmailMessageTemplate += "</body></html>";

			List<SettingInfo> companySettings = GetSettings();

			string fromName = companySettings.First(s => s.ID == Setting.WorkflowFromName).Value;
			string fromEmail = companySettings.First(s => s.ID == Setting.WorkflowFromEmail).Value;

			if (settings.RecipientType == EmailTaskRecipientType.SpecificUser)
			{
				if (string.IsNullOrEmpty(settings.SpecificUser))
				{
					Console.Write("ERROR - NO USER SPECIFIED FOR THE SPECIFIC USER EMAIL TASK.");

					return;
				}

				foreach (string email in settings.SpecificUser.Split(';'))
				{
					Console.WriteLine($"DEBUG : WORKFLOW AGGREGATE EMAIL IS EMAILING [{email}].");

					await Mail.SendMessage(settings.EmailHeader, email, "", settings.EmailMessageTemplate, true, fromEmail, fromName);
				}
			}
		}

		private async Task<bool> SendWorkflowEmail(WorkflowItemStep item, EventInfo eventInfo, WorkflowItemStepSettingModel settings)
		{
			List<string> emailedUsers = new List<string>();
			EventObjectInfo objectInfo = eventInfo.Object;

			if (string.IsNullOrEmpty(item.Step.Settings))
			{
				throw new ArgumentException("INVALID EMAIL CONFIGURATION FOR SPECIFIED STEP.");
			}

			string url = "";
			string prefix = Community.GetPrimaryUrlPrefix();

			url += $"https://{prefix}.data3sixty.com/workflow/details/{item.ItemID}";

			settings.BodyTemplate = await ProcessMessageTokens(settings.BodyTemplate, objectInfo, prefix, item);
			settings.SubjectTemplate = await ProcessMessageTokens(settings.SubjectTemplate, objectInfo, prefix, item, false);

			settings.BodyTemplate = $"<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"><title></title></head><body style=\"font-family:trebuchet ms,helvetica,sans-serif;\">{settings.BodyTemplate}<p>Item Workflow Details {url}</p>";

			List<SettingInfo> companySettings = GetSettings();

			string fromName = companySettings.First(s => s.ID == Setting.WorkflowFromName).Value;
			string fromEmail = companySettings.First(s => s.ID == Setting.WorkflowFromEmail).Value;

			//if the setting to include responses from froms is enabled then get previous form responses and put in xml
			if (settings.ShouldIncludeFormResponses)
			{
				settings.BodyTemplate += GenerateFormResponsesEmailContent(item.ItemID);
			}

			settings.BodyTemplate += "</body></html>";

			if (settings.RecipientType == EmailTaskRecipientType.Initiator)
			{
				if (item.Item.StartedBy <= 0)
				{
					Console.WriteLine("ERROR CANNOT DETERMINE WHO TO EMAIL WORKLFOW EMAIL TASK MESSAGE TO.");

					return true;
				}

				GlobalReportingResource res = GlobalReportingResources.Where(x => x.ResourceID == item.Item.StartedBy).FirstOrDefault();

				if (res == null)
				{
					Console.WriteLine("ERROR CANNOT FIND THE RESOURCE WHO STARTED THE WORKFLOW TO EMAIL.");

					return true;
				}

				Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{res.Email}].");
				emailedUsers.Add(res.Email);

				try
				{
					await Mail.SendMessage(settings.SubjectTemplate, res.Email, res.FirstName + " " + res.LastName, settings.BodyTemplate, true, fromEmail, fromName);
				}
				catch (Exception e)
				{
					//error sending email
					TelemetryClient client = new TelemetryClient();
					client.TrackException(e, new Dictionary<string, string> { { "CompanyID", CurrentCompanyID.ToString() } });
				}
			}
			else if (settings.RecipientType == EmailTaskRecipientType.Responsibility)
			{
				if (await ShouldWaitForResponsibilityRuleToRun(item, settings, item.ID, eventInfo))
				{
					return false;
				}

				IEnumerable<GlobalReportingResource> users = GetWorkflowUsersBasedOnResponsibility(item.Step.Version.TypeID, item.Step.ID, item.ItemID, settings.SendToDefaultUsers);

				foreach (GlobalReportingResource user in users)
				{
					Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{user.Email}].");

					emailedUsers.Add(user.Email);

					try
					{
						await Mail.SendMessage(settings.SubjectTemplate, user.Email, user.FirstName + " " + user.LastName, settings.BodyTemplate, true, fromEmail, fromName);
					}
					catch (Exception e)
					{
						//error sending email
						TelemetryClient client = new TelemetryClient();
						client.TrackException(e, new Dictionary<string, string> { { "CompanyID", CurrentCompanyID.ToString() } });
					}
				}
			}
			else if (settings.RecipientType == EmailTaskRecipientType.Followers)
			{
				IEnumerable<GlobalReportingResource> users = GetWorkflowUsersBasedOnFollowers(item.Step.Version.TypeID, item.Step.ID, item.ItemID);

				foreach (GlobalReportingResource user in users)
				{
					Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{user.Email}].");

					emailedUsers.Add(user.Email);

					try
					{
						await Mail.SendMessage(settings.SubjectTemplate, user.Email, user.FirstName + " " + user.LastName, settings.BodyTemplate, true, fromEmail, fromName);
					}
					catch (Exception e)
					{
						//error sending email
						TelemetryClient client = new TelemetryClient();
						client.TrackException(e, new Dictionary<string, string> { { "CompanyID", CurrentCompanyID.ToString() } });
					}
				}
			}
			else if (settings.RecipientType == EmailTaskRecipientType.SpecificUser)
			{
				if (string.IsNullOrEmpty(settings.SpecificUser))
				{
					Console.Write("ERROR - NO USER SPECIFIED FOR THE SPECIFIC USER EMAIL TASK.");

					return true;
				}

				Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{settings.SpecificUser}].");

				foreach (string email in settings.SpecificUser.Split(';'))
				{
					emailedUsers.Add(email);

					try
					{
						await Mail.SendMessage(settings.SubjectTemplate, email, "", settings.BodyTemplate, true, fromEmail, fromName);
					}
					catch (Exception e)
					{
						//error sending email
						TelemetryClient client = new TelemetryClient();
						client.TrackException(e, new Dictionary<string, string> { { "CompanyID", CurrentCompanyID.ToString() } });
					}
				}
			}
			else if (settings.RecipientType == EmailTaskRecipientType.Group)
			{
				if (settings.RecipientGroup == Guid.Empty)
				{
					Console.Write("ERROR - NO GROUP SPECIFIED FOR THE GROUP EMAIL TASK.");
					
					return true;
				}

				int recipientGroup = Query<int>(@"select ObjectID from asset where [Object] = 'Group' and uid = @Uid", new { Uid = settings.RecipientGroup }).FirstOrDefault();
				
				if (recipientGroup <= 0)
				{
					Console.Write("ERROR - INVALID GROUP FOR THE GROUP EMAIL TASK.");

					return true;
				}

				IEnumerable<GlobalReportingResource> users = GetWorkflowUsersBasedOnGroup(recipientGroup);

				foreach (GlobalReportingResource user in users)
				{
					Console.WriteLine($"DEBUG : EMAIL STEP IS EMAILING [{user.Email}].");

					emailedUsers.Add(user.Email);

					try
					{
						await Mail.SendMessage(settings.SubjectTemplate, user.Email, user.FirstName + " " + user.LastName, settings.BodyTemplate, true, fromEmail, fromName);
					}
					catch (Exception e)
					{
						//error sending email
						TelemetryClient client = new TelemetryClient();
						client.TrackException(e, new Dictionary<string, string> { { "CompanyID", CurrentCompanyID.ToString() } });
					}
				}
			}

			SaveItemStepEmailedUsers(item, emailedUsers);

			return true;
		}

		public string GenerateFormResponsesEmailContent(long itemId)
		{
			IQueryable<WorkflowItemStep> formResponses = WorkflowItemSteps.Where(x => x.ItemID == itemId && x.Step.ActivityType == WorkflowActivityType.Form).Include(x => x.Step);

			StringBuilder sb = new StringBuilder();
			sb.Append($"<br><br><b>Form responses</b><br>");

			foreach (WorkflowItemStep formResponse in formResponses)
			{
				if (string.IsNullOrEmpty(formResponse.Fields))
				{
					continue;
				}

				XElement xml = XElement.Parse(formResponse.Fields);

				string name = formResponse.Step != null ? formResponse.Step.Name : "(unknown)";

				if (xml.Elements("form") != null && xml.Elements("form").Any())
				{
					sb.Append($"<br><br>{name}");
				}

				foreach (XElement form in xml.Elements("form"))
				{

					if (int.TryParse((string)form.Attribute("ResourceID"), out int resourceID))
					{
						GlobalReportingResource user = GlobalReportingResources.Where(x => x.ResourceID == resourceID).FirstOrDefault();

						if (user != null)
						{
							sb.Append($"<br>Response from user <b>{user.FullName}</b><br>");
						}
					}

					foreach (XElement field in form.Elements("field"))
					{
						string fieldName = (string)field.Attribute("label");
						string value = (string)field.Attribute("value");
						string fieldType = (string)field.Attribute("fieldtype");


						if ((fieldType ?? "").ToUpperInvariant() == "RELATIONSHIPTYPE")
						{
							value = (string)field.Attribute("displayvalue");

							if (!string.IsNullOrEmpty(value))
							{
								List<string> objects = value.Split(',').ToList();
								List<string> objectNames = new List<string>();

								foreach (string o in objects)
								{
									string type = o.Split('|')[0];
									if (int.TryParse(o.Split('|')[1], out int id))
									{
										ObjectDetail objDetail = GetObjectDetail(type.Replace("Type", ""), id);
										if (objDetail != null)
										{
											objectNames.Add(objDetail.Name);
										}
									}
								}

								value = string.Join(", ", objectNames);
							}
						}

						if ((fieldType ?? "").ToUpperInvariant() == "LIST")
						{
							value = (string)field.Attribute("displayvalue");
						}

						if ((fieldType ?? "").ToUpperInvariant() == "DATE")
						{
							if (DateTime.TryParse(value, out DateTime dt))
							{
								value = dt.ToShortDateStringInvariantCulture();
							}
						}

						sb.Append($"<b>{fieldName}</b> {value}<br>");
					}
				}
			}

			return sb.ToString();
		}

		private bool GetWorkflowResponsibilityHasUsers(int typeID, int stepID, long itemID)
		{
			return Query<GlobalReportingResource>("[utility].[GetOwnersForWorkflow] @id, @stepId, @itemId", new { id = typeID, @stepId = stepID, @itemId = itemID }).Any();
		}

		private int GetWorkflowAdminGroup()
		{

			List<SettingInfo> companySettings = GetSettings();
			string defaultGroup = companySettings.First(s => s.ID == Setting.WorkflowCatchAllGroup).Value;

			int.TryParse(defaultGroup, out int defaultWorkflowUserGroup);

			return defaultWorkflowUserGroup;
		}

		public IEnumerable<GlobalReportingResource> GetWorkflowUsersBasedOnResponsibility(int typeID, int stepID, long itemID, bool sendToDefaultUsers = true)
		{
			IEnumerable<GlobalReportingResource> users = Query<GlobalReportingResource>("[utility].[GetOwnersForWorkflow] @id, @stepId, @itemId", new { id = typeID, @stepId = stepID, @itemId = itemID });

			if (users == null || users.Count() == 0)
			{
				if (sendToDefaultUsers == false)
				{
					return new List<GlobalReportingResource>();
				}

				//check if there is a system setting that says to use a group.
				int defaultWorkflowUserGroup = GetWorkflowAdminGroup();

				if (defaultWorkflowUserGroup > 0)
				{
					// a default workflow group has been defined for when there are no memebers in the resonponsibilities
					return GetWorkflowUsersBasedOnGroup(defaultWorkflowUserGroup);
				}
				else
				{
					// else add all admins
					// no default workflow email group defined
					return Query<GlobalReportingResource>(@"select	R.ResourceID, 
									R.FirstName, 
									R.LastName, 
									R.Email, 
									R.Email, 
									R.LastLoggedInOn,
									R.LastLoggedInOn as DateLastLoggedIn, 
									R.[State],
									case R.[State] when 1 then 'Active' else 'Inactive' end as [Status] 
							from	reporting.Global_Resource R where isadministrator = 1 and R.[State] = 1");
				}
			}

			return users;
		}

		public IEnumerable<GlobalReportingResource> GetWorkflowUsersBasedOnFollowers(int typeID, int stepID, long itemID)
		{
			IEnumerable<GlobalReportingResource> users = Query<GlobalReportingResource>("[utility].[GetOwnersForWFFollowers] @id, @stepId, @itemId", new { id = typeID, @stepId = stepID, @itemId = itemID });

			return users;
		}

		public IEnumerable<GlobalReportingResource> GetWorkflowUsersBasedOnGroup(int groupId)
		{
			// a default workflow group has been defined for when there are no memebers in the resonponsibilities
			return Query<GlobalReportingResource>(@"select distinct	R.ResourceID, 
							R.FirstName, 
							R.LastName, 
							R.Email, 
							R.Email, 
							R.LastLoggedInOn,
							R.LastLoggedInOn as DateLastLoggedIn, 
							R.[State],
							case R.[State] when 1 then 'Active' else 'Inactive' end as [Status] 
					from	reporting.Global_Resource R
					inner join [resourcegroup] rg on (R.ResourceID = rg.ResourceID)
					where rg.groupid= @groupId and R.[State] = 1", new { groupId });
		}

		private void SaveItemStepEmailedUsers(WorkflowItemStep item, List<string> emailedUsers)
		{

			//save the emailed users to the settings
			if (emailedUsers.Count > 0 && !string.IsNullOrEmpty(item.Settings))
			{
				XElement root = XElement.Parse(item.Settings);

				XElement emailForm = new XElement("emails");

				foreach (string email in emailedUsers)
				{
					emailForm.Add(new XElement("email",
							new XAttribute("address", email)));
				}

				root.Add(emailForm);

				item.Settings = root.ToString();
				SaveChanges();
			}
		}

		private async Task SaveHttpResponseResultsAsync(WorkflowItemStep item, WorkflowHttpRequestSettingsModel settings, HttpResponseMessage response)
		{
			if (!string.IsNullOrEmpty(item.Fields))
			{
				XElement root = XElement.Parse(item.Fields);
				XElement xResponse = new XElement("HTTPResponse");

				if (response == null)
				{
					xResponse.Add(new XElement("StatusCode", 0));
					xResponse.Add(new XElement("Body", ""));
				}
				else
				{
					xResponse.Add(new XElement("StatusCode", (int)response.StatusCode));
					xResponse.Add(new XElement("Body", await response.Content.ReadAsStringAsync()));
				}

				root.Add(xResponse);
				item.Fields = root.ToString();
			}

			if (!string.IsNullOrEmpty(item.Settings))
			{
				XElement root = XElement.Parse(item.Settings);

				XElement xRequest = new XElement("HTTPRequest");
				xRequest.Add(new XElement("Url", settings.FormattedUrl?.ToString() ?? ""));
				xRequest.Add(new XElement("Method", settings.Method.ToUpper()));
				xRequest.Add(new XElement("Timeout", settings.Timeout.ToString()));

				if (settings?.Headers?.Any() == true)
				{
					foreach (WorkflowHttpRequestHeader header in settings.Headers)
					{
						XElement h = new XElement("Headers");
						h.Add(new XElement("Key", header.Key));
						h.Add(new XElement("Value", header.Value));
						xRequest.Add(h);
					}
				}
				else
				{
					xRequest.Add(new XElement("Headers", null));
				}

				xRequest.Add(new XElement("Body", settings.Body));

				root.Add(xRequest);
				item.Settings = root.ToString();
			}

			await SaveChangesAsync();
		}

		public async Task<string> ProcessMessageTokens(string bodyTemplate, EventObjectInfo objectInfo, string prefix, WorkflowItemStep itemStep, bool supportHtml = true, bool forJson = false, bool lookupFieldsPassedByValue = false)
		{
			return await ProcessMessageTokens(bodyTemplate, objectInfo.ObjectID, objectInfo.Object, prefix, itemStep, supportHtml, forJson, lookupFieldsPassedByValue);
		}

		public async Task<string> ProcessMessageTokens(string bodyTemplate, int objectID, SystemObjects obj, string prefix, WorkflowItemStep itemStep, bool supportHtml, bool forJson, bool lookupFieldsPassedByValue)
		{
			if (string.IsNullOrEmpty(bodyTemplate))
			{
				return string.Empty;
			}

			if (supportHtml)
			{
				Ganss.XSS.HtmlSanitizer sanitizer = new Ganss.XSS.HtmlSanitizer();
				sanitizer.AllowedSchemes.Add("data");
				bodyTemplate = sanitizer.Sanitize(bodyTemplate);
			}

			string result = bodyTemplate;

			//replace [OBJECT_NAME] with the object name            
			if (result.Contains("[OBJECT_NAME]"))
			{
				ObjectDetail item = null;
				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						item = GetObjectDetail(issue.Object, issue.ObjectID);
					}
				}
				else
				{
					//get the objects name
					item = GetObjectDetail(obj.ToString(), objectID);
				}

				string itemLink = "(unknown item)";

				if (item != null)
				{
					if (supportHtml)
					{
						itemLink = $"<b><a href=\"https://{prefix}.data3sixty.com/{item.Url}\">{item.Name}</a></b>";
					}
					else
					{
						itemLink = item.Name;
					}
				}

				result = result.Replace("[OBJECT_NAME]", itemLink);
			}

			if (result.Contains("[REL_OBJECT_NAME]"))
			{
				string itemLink = "";

				if (obj == SystemObjects.Intersect)
				{
					Intersect intersect = Intersects.Where(i => i.ID == objectID).FirstOrDefault();

					if (intersect != null)
					{
						ObjectDetail item = GetObjectDetail(intersect.Object, intersect.ObjectID);

						if (item != null)
						{
							if (supportHtml)
							{
								itemLink = $"<b><a href=\"https://{prefix}.data3sixty.com/{item.Url}\">{item.Name}</a></b>";
							}
							else
							{
								itemLink = item.Name;
							}
						}
					}
				}

				result = result.Replace("[REL_OBJECT_NAME]", itemLink);
			}

			if (result.Contains("[REL_SUBJECT_NAME]"))
			{
				string itemLink = "";

				if (obj == SystemObjects.Intersect)
				{
					Intersect intersect = Intersects.Where(i => i.ID == objectID).FirstOrDefault();

					if (intersect != null)
					{
						ObjectDetail item = GetObjectDetail(intersect.Subject, intersect.SubjectID);

						if (item != null)
						{
							if (supportHtml)
							{
								itemLink = $"<b><a href=\"https://{prefix}.data3sixty.com/{item.Url}\">{item.Name}</a></b>";
							}
							else
							{
								itemLink = item.Name;
							}
						}
					}
				}

				result = result.Replace("[REL_SUBJECT_NAME]", itemLink);
			}

			if (result.Contains("[ACTION_DETAILS]"))
			{
				//get the details of the issue and add them in
				Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

				var issueInfo = new StringBuilder();
				issueInfo.Append("(unknown)");

				if (issue != null)
				{
					ObjectDetail item = GetObjectDetail(issue.Object, issue.ObjectID);

					if (item != null)
					{
						issueInfo.Clear();
						issueInfo.Append($"New Action Type <b>{issue.IssueType.Name}</b> Raised on <b>{item.Name}</b>.");
					}

					GlobalReportingResource creator = GlobalReportingResources.Where(x => x.ResourceID == issue.CreatedBy).FirstOrDefault();

					if (creator != null)
					{
						issueInfo.Append($"<br>Created By <b>{creator.FullName}</b>");
					}

					//get any field values for this issue
					IOrderedQueryable<FieldType> fieldTypes = FieldTypes
						.Where(x => x.Object == "IssueType" && x.ObjectID == issue.IssueTypeID)
						.OrderBy(x => x.ColumnOrder)
						.ThenBy(x => x.FriendlyName);

					IQueryable<Field> fieldValues = Fields.Where(x => x.ObjectType == "Issue" && x.ObjectID == issue.ID);

					foreach (FieldType fieldType in fieldTypes)
					{
						Field field = fieldValues.Where(x => x.FieldTypeID == fieldType.ID).FirstOrDefault();

						if (field != null)
						{
							string type = fieldType.Type;
							string ConvertFormattedValue;

							if (type == "Date" || type == "DateTime")
							{
								ConvertFormattedValue = GetDisplayDateTimeValue(type, field.FormattedValue);
							}
							else
							{
								ConvertFormattedValue = field.FormattedValue;
							}

							issueInfo.Append($"<br><b>{fieldType.FriendlyName}</b>: {ConvertFormattedValue}");
						}
					}
				}

				result = result.Replace("[ACTION_DETAILS]", issueInfo.ToString());
			}

			if (result.Contains("[WORKFLOW_INITIATOR]"))
			{
				string initiator = "unknown user";

				if (itemStep.Item != null && itemStep.Item.StartedBy > 0)
				{
					GlobalReportingResource user = GlobalReportingResources.Where(x => x.ResourceID == itemStep.Item.StartedBy).FirstOrDefault();

					if (user != null)
					{
						initiator = user.FullName;
					}
				}

				result = result.Replace("[WORKFLOW_INITIATOR]", initiator);
			}

			if (result.Contains("[WORKFLOW_INITIATOR_UID]"))
			{
				Guid initiator = Guid.Empty;

				if (itemStep.Item != null && itemStep.Item.StartedBy > 0)
				{
					GlobalReportingResource user = GlobalReportingResources.Where(x => x.ResourceID == itemStep.Item.StartedBy).FirstOrDefault();

					if (user != null)
					{
						initiator = user.Uid;
					}
				}

				result = result.Replace("[WORKFLOW_INITIATOR_UID]", initiator.ToString());
			}

			//need to keep both options for existing workflows, remove [SCORE] once no workflow use it in any ENV
			if (result.Contains("[GOV_SCORE]") || result.Contains("[SCORE]"))
			{
				ObjectDetail item = null;

				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						item = GetObjectDetail(issue.Object, issue.ObjectID);
					}
				}
				else
				{
					//get the objects name
					item = GetObjectDetail(obj.ToString(), objectID);
				}
				decimal? score = null;

				if (item != null && item.AssetID.HasValue)
				{
					score = GetAssetScore(item.AssetID.Value, ScoreType.Governance);
				}

				result = result.Replace("[GOV_SCORE]", score.HasValue ? $"{score.Value.ToString("0.#")}%" : "(unknown score)");
				result = result.Replace("[SCORE]", score.HasValue ? $"{score.Value.ToString("0.#")}%" : "(unknown score)");
			}

			if (result.Contains("[DQ_SCORE]"))
			{
				ObjectDetail item = null;

				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						item = GetObjectDetail(issue.Object, issue.ObjectID);
					}
				}
				else
				{
					//get the objects name
					item = GetObjectDetail(obj.ToString(), objectID);
				}
				decimal? score = null;

				if (item != null && item.AssetID.HasValue)
				{
					score = GetAssetScore(item.AssetID.Value, ScoreType.DataQuality);
				}

				result = result.Replace("[DQ_SCORE]", score.HasValue ? $"{score.Value.ToString("0.#")}%" : "(unknown score)");
			}

			if (result.Contains("[DQ_SCORE_PREV]"))
			{
				ObjectDetail item = null;
				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						item = GetObjectDetail(issue.Object, issue.ObjectID);
					}
				}
				else
				{
					//get the objects name
					item = GetObjectDetail(obj.ToString(), objectID);
				}
				decimal? score = null;

				if (item != null && item.AssetID.HasValue)
				{
					score = GetPreviousAssetScore(item.AssetID.Value, ScoreType.DataQuality);
				}

				result = result.Replace("[DQ_SCORE_PREV]", score.HasValue ? $"{score.Value.ToString("0.#")}%" : "(No prior score)");
			}

			if (result.Contains("[GOV_SCORE_PREV]"))
			{
				ObjectDetail item = null;

				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						item = GetObjectDetail(issue.Object, issue.ObjectID);
					}
				}
				else
				{
					//get the objects name
					item = GetObjectDetail(obj.ToString(), objectID);
				}

				decimal? score = null;

				if (item != null && item.AssetID.HasValue)
				{
					score = GetPreviousAssetScore(item.AssetID.Value, ScoreType.Governance);
				}

				result = result.Replace("[GOV_SCORE_PREV]", score.HasValue ? $"{score.Value.ToString("0.#")}%" : "(No prior score)");
			}

			if (Regex.IsMatch(result, "\\[FIELD([0-9.]+)\\]"))
			{
				MatchCollection fields = Regex.Matches(result, "\\[FIELD([0-9.]+)\\]");

				foreach (object field in fields)
				{
					string item = field.ToString();


					string fieldIdStringitem = item.Replace("[FIELD", "");
					fieldIdStringitem = fieldIdStringitem.Replace("]", "");

					int.TryParse(fieldIdStringitem, out int fieldId);

					string fieldValue = "";

					if (fieldId > 0)
					{
						Field fieldRecord = Fields.Where(x => x.ObjectID == objectID && x.ObjectType == obj.ToString() && x.FieldTypeID == fieldId).FirstOrDefault();

						//If there is no field and type is Issue, this might be asset field
						if (fieldRecord == null && obj == SystemObjects.Issue)
						{
							Issue issue = Issues.FirstOrDefault(x => x.ID == objectID);
							fieldRecord = Fields.Where(x => x.ObjectID == issue.ObjectID && x.ObjectType == issue.Object && x.FieldTypeID == fieldId).FirstOrDefault();
						}

						if ((obj.ToString() ?? "").ToUpper() == "INTERSECT")
						{
							Intersect intersect = Intersects.Where(i => i.ID == objectID).FirstOrDefault();

							if (intersect != null)
							{
								Field ofieldRecord = Fields.Where(x => x.ObjectID == intersect.ObjectID && x.ObjectType == intersect.Object && x.FieldTypeID == fieldId).FirstOrDefault();

								if (ofieldRecord != null)
								{
									fieldValue = ofieldRecord.FormattedValue;
								}

								Field sfieldRecord = Fields.Where(x => x.ObjectID == intersect.SubjectID && x.ObjectType == intersect.Subject && x.FieldTypeID == fieldId).FirstOrDefault();

								if (!string.IsNullOrEmpty(fieldValue))
								{
									fieldValue += " ";
								}

								if (sfieldRecord != null)
								{
									fieldValue = sfieldRecord.FormattedValue;
								}
							}
						}

						if (fieldRecord != null)
						{
							FieldType fieldType = FieldTypes.Where(x => x.ID == fieldRecord.FieldTypeID).FirstOrDefault();
							
							if (fieldType != null)
							{
								DateTime dateValue;
								string type = fieldType.Type;

								if (type == "Date")
								{
									if (DateTime.TryParseExact(fieldRecord.FormattedValue, "M/d/yyyy h:mm:ss tt", CultureInfo.CurrentCulture, DateTimeStyles.None, out dateValue))
									{
										string formattedDate = dateValue.ToString("dd MMM yyyy");
										fieldValue = formattedDate;
									}
									else if (DateTime.TryParseExact(fieldRecord.FormattedValue, "MM/dd/yyyy HH:mm:ss", null, DateTimeStyles.None, out dateValue))
									{
										string formattedDate = dateValue.ToString("dd MMM yyyy");
										fieldValue = formattedDate;
									}
									else if (DateTime.TryParseExact(fieldRecord.FormattedValue, "M/d/yyyy", null, DateTimeStyles.None, out dateValue))
									{
										string formattedDate = dateValue.ToString("dd MMM yyyy");
										fieldValue = formattedDate;
									}
									else if (DateTime.TryParseExact(fieldRecord.FormattedValue, "MM/dd/yyyy", null, DateTimeStyles.None, out dateValue))
									{
										string formattedDate = dateValue.ToString("dd MMM yyyy");
										fieldValue = formattedDate;
									}
									else
									{
										fieldValue = fieldRecord.FormattedValue;
									}
								}
								else if (type == "DateTime")
								{
									if (DateTime.TryParse(fieldRecord.FormattedValue, out dateValue))
									{
										string formattedDate = dateValue.ToString("dd MMM yyyyTHH:mm:ss");
										fieldValue = formattedDate;
									}
									else
									{
										fieldValue = fieldRecord.FormattedValue;
									}
								}
								else if (forJson)
								{
									string fieldValuetemp = "";
									if (type == "Lookup")
									{
										if (lookupFieldsPassedByValue)
										{
											fieldValuetemp = fieldRecord.Value;
										}
										else
										{
											fieldValuetemp = fieldRecord.FormattedValue;
										}

										if (string.IsNullOrEmpty(fieldValuetemp))
										{
											fieldValuetemp = fieldRecord.FormattedValue;
										}
									}
									else
									{
										fieldValuetemp = fieldRecord.FormattedValue;
									}
									fieldValue = JsonConvert.ToString(fieldValuetemp);

									if (!string.IsNullOrEmpty(fieldValue))
									{
										fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
									}
								}
								else
								{
									fieldValue = fieldRecord.FormattedValue;
								}
							}
							else
							{
								fieldValue = fieldRecord.FormattedValue;
							}
						}
					}

					result = result.Replace(item, fieldValue);
				}
			}

			if (Regex.IsMatch(result, "\\[JSON([0-9.]+)\\]"))
			{
				MatchCollection fields = Regex.Matches(result, "\\[JSON([0-9.]+)\\]");

				foreach (object field in fields)
				{
					string item = field.ToString();
					string fieldValue = "";

					string fieldTypeIdStringitem = item.Replace("[JSON", "");
					fieldTypeIdStringitem = fieldTypeIdStringitem.Replace("]", "");

					int.TryParse(fieldTypeIdStringitem, out int fieldTypeId);

					FieldType fieldType = FieldTypes.Where(x => x.ID == fieldTypeId).FirstOrDefault();

					FieldTypeDefinition_JsonElement jsonElementDefinition = null;

					if (fieldType != null && fieldType.Type == DataType.JsonElement.ToString())
					{
						jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(fieldType.Definition);

						Field fieldRecord = Fields.Where(x => x.ObjectID == objectID && x.ObjectType == obj.ToString() && x.FieldTypeID == jsonElementDefinition.FieldTypeID).FirstOrDefault();

						JObject fielddata = JObject.Parse(fieldRecord.Value);

						fieldValue = fielddata.SelectToken(jsonElementDefinition.Path, false)?.ToString() ?? "";
					}

					if (forJson)
					{
						fieldValue = JsonConvert.ToString(fieldValue);

						if (!string.IsNullOrEmpty(fieldValue))
						{
							fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
						}
					}

					result = result.Replace(item, fieldValue);
				}
			}

			if (Regex.IsMatch(result, "\\[HTTPREQUEST\\|([0-9.]+)\\|([a-zA-Z]+)\\]"))
			{
				MatchCollection fields = Regex.Matches(result, "\\[HTTPREQUEST\\|([0-9.]+)\\|([a-zA-Z]+)\\]");

				foreach (object field in fields)
				{
					string item = field.ToString();

					string fieldTypeIdStringitem = item.Replace("[HTTPREQUEST|", "");
					fieldTypeIdStringitem = fieldTypeIdStringitem.Replace("]", "");

					int stepId = -1;
					string property = fieldTypeIdStringitem.Split('|')[1];

					int.TryParse(fieldTypeIdStringitem.Split('|')[0], out stepId);

					WorkflowItemStep step = WorkflowItemSteps.FirstOrDefault(s => s.StepID == stepId && s.ItemID == itemStep.ItemID);

					if (step != null)
					{
						XElement response = step.FieldsDocument;
						response = response.Element("HTTPResponse");

						if (response != null)
						{
							switch (property.ToUpperInvariant())
							{
								case "STATUSCODE":
									result = result.Replace(item, response.Element("StatusCode")?.Value ?? "");
									break;
								case "RESPONSEBODY":
									result = result.Replace(item, response.Element("Body")?.Value ?? "");
									break;
								default:
									//Do nothing.
									break;
							}
						}
					}
				}
			}

			if (Regex.IsMatch(result, "\\[HTTPRESPONSE\\|([0-9.]+)\\|([0-9.]+)\\]"))
			{
				MatchCollection fields = Regex.Matches(result, "\\[HTTPRESPONSE\\|([0-9.]+)\\|([0-9.]+)\\]");

				foreach (object field in fields)
				{
					string item = field.ToString();

					string fieldTypeIdStringitem = item.Replace("[HTTPRESPONSE|", "");
					fieldTypeIdStringitem = fieldTypeIdStringitem.Replace("]", "");

					int stepId = -1;
					string fieldId = fieldTypeIdStringitem.Split('|')[1];
					int.TryParse(fieldTypeIdStringitem.Split('|')[0], out stepId);

					result = result.Replace(item, GetOutputFieldValue(stepId, itemStep.ItemID, fieldId));
				}
			}

			if (result.Contains("[OBJECT_TYPE]"))
			{
				string path = null;

				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						path = GetObjectTypePath(issue.Object, issue.ObjectID);
					}
				}
				else
				{
					//get the objects name
					path = GetObjectTypePath(obj.ToString(), objectID);
				}

				string itemLink = "(unknown item)";

				if (path != null)
				{
					if (supportHtml)
					{
						itemLink = $"<b>{path}</b>";
					}
					else
					{
						itemLink = path;
					}
				}

				result = result.Replace("[OBJECT_TYPE]", itemLink);
			}

			if (result.Contains("[WORKFLOW_STEP_ID]"))
			{
				result = result.Replace("[WORKFLOW_STEP_ID]", itemStep.StepID.ToString());
			}

			if (result.Contains("[WORKFLOW_INSTANCE_ID]"))
			{
				result = result.Replace("[WORKFLOW_INSTANCE_ID]", itemStep.ItemID.ToString());
			}

			if (result.Contains("[WORKFLOW_ID]"))
			{
				WorkflowVersionStep versionStep = WorkflowVersionSteps.FirstOrDefault(s => s.ID == itemStep.StepID);

				if (versionStep != null)
				{
					WorkflowVersion version = WorkflowVersions.FirstOrDefault(v => v.ID == versionStep.VersionID);
					result = result.Replace("[WORKFLOW_ID]", version?.TypeID.ToString() ?? "");
				}
			}

			if (result.Contains("[RECIPIENT_RESPONSIBILITY]"))
			{
				string recipientResponsibility = "";
				if (itemStep != null && itemStep.Step != null)
				{
					WorkflowItemStepSettingModel stepSettings = WorkflowItemStepSettingModel.ParseXml(itemStep.Step.Settings);

					if (stepSettings.RecipientType == EmailTaskRecipientType.Responsibility)
					{
						recipientResponsibility = await GetWorkflowAssignedResponsibility(itemStep.Step.Version.TypeID, itemStep.Step.ID, itemStep.ItemID);
					}

					result = result.Replace("[RECIPIENT_RESPONSIBILITY]", recipientResponsibility);
				}
			}

			if (result.Contains("[RECIPIENT_TYPE]"))
			{
				string recipientType = "";
				if (itemStep != null && itemStep.Step != null)
				{
					WorkflowItemStepSettingModel stepSettings = WorkflowItemStepSettingModel.ParseXml(itemStep.Step.Settings);

					switch (stepSettings.RecipientType)
					{
						case EmailTaskRecipientType.Initiator:
							recipientType = "Initiator";
							break;
						case EmailTaskRecipientType.Responsibility:
							recipientType = "Responsibility";
							break;
						case EmailTaskRecipientType.SpecificUser:
							recipientType = "Specific User";
							break;
						default:
							//Nothing to do here.
							break;
					}

					//check how many users would recieve this if responsibility
					if (stepSettings.RecipientType == EmailTaskRecipientType.Responsibility && !GetWorkflowResponsibilityHasUsers(itemStep.Step.Version.TypeID, itemStep.Step.ID, itemStep.ItemID))
					{
						int adminGroupId = GetWorkflowAdminGroup();

						if (adminGroupId <= 0)
						{
							recipientType = "Default - Administrators";
						}
						else
						{
							AssetDetail group = AssetDetails.FirstOrDefault(a => a.ObjectID == adminGroupId && a.Object == "Group");

							if (group != null)
							{
								recipientType = $"Default - {group.DisplayValue}";
							}
							else
							{
								recipientType = $"Default - (unknown group)";
							}
						}
					}
				}
				result = result.Replace("[RECIPIENT_TYPE]", recipientType);
			}

			if (result.Contains("[ASSET_PATH]"))
			{

				ObjectDetail item = null;

				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						item = GetObjectDetail(issue.Object, issue.ObjectID);
					}
				}
				else
				{
					//get the objects name
					item = GetObjectDetail(obj.ToString(), objectID);
				}

				string path = item?.UID == null ? null : Query<string>(@"select graph.GetPath(AN.Segments, ' > ', ' / ') from graph.assetNode AN where AN.Uid = @Uid", new { Uid = item.UID }).FirstOrDefault();

				result = result.Replace("[ASSET_PATH]", path ?? "(unknown)");
			}

			if (result.Contains("[ASSET_UID]"))
			{
				string uid = null;

				if (obj == SystemObjects.Issue)
				{
					Issue issue = Issues.Where(i => i.ID == objectID).Include(x => x.IssueType).FirstOrDefault();

					if (issue != null)
					{
						Asset issueAsset = Assets.Where(x => x.Object == issue.Object && x.ObjectID == issue.ObjectID).FirstOrDefault();

						if (issueAsset != null)
						{
							uid = issueAsset.uid.ToString();
						}
					}
				}
				else if (obj == SystemObjects.Intersect)
				{
					Intersect intersect = Intersects.Where(i => i.ID == objectID).FirstOrDefault();

					if (intersect != null)
					{
						uid = intersect.uid.ToString();
					}
				}
				else
				{
					Asset asset = Assets.Where(x => x.Object == obj.ToString() && x.ObjectID == objectID).FirstOrDefault();

					if (asset != null)
					{
						uid = asset.uid.ToString();
					}
				}

				result = result.Replace("[ASSET_UID]", (uid ?? "(unknown item)"));
			}

			if (result.Contains("[REL_SUBJECT_UID]"))
			{
				string uid = null;

				if (obj == SystemObjects.Intersect)
				{
					Intersect intersect = Intersects.Where(i => i.ID == objectID).FirstOrDefault();

					if (intersect != null)
					{
						ObjectDetail item = GetObjectDetail(intersect.Subject, intersect.SubjectID);

						if (item != null)
						{
							uid = item.UID.ToString();
						}
					}
				}

				result = result.Replace("[REL_SUBJECT_UID]", uid ?? "(unknown intersect)");
			}

			if (result.Contains("[REL_OBJECT_UID]"))
			{
				string uid = null;
				if (obj == SystemObjects.Intersect)
				{
					Intersect intersect = Intersects.Where(i => i.ID == objectID).FirstOrDefault();

					if (intersect != null)
					{
						ObjectDetail item = GetObjectDetail(intersect.Object, intersect.ObjectID);

						if (item != null)
						{
							uid = item.UID.ToString();
						}
					}
				}

				result = result.Replace("[REL_OBJECT_UID]", uid ?? "(unknown intersect)");
			}

			return result;
		}


		/// <summary>
		/// Gets the active workflow item step based on a given ID.
		/// </summary>
		/// <param name="itemStepID">The item ID</param>
		/// <returns>An active workflow item step model.</returns>
		private WorkflowItemStep getWorkflowItemStep(long itemStepID, bool isStepCompleted = false)
		{
			WorkflowItemStep itemStep = WorkflowItemSteps.Include(i => i.Step).SingleOrDefault(i => i.ID == itemStepID);

			if (itemStep == null)
			{
				throw new ArgumentNullException(nameof(itemStep), "Item Step ID does not correspond to a valid workflow item step.");
			}

			if (!isStepCompleted && itemStep.CompletedOn.HasValue)
			{
				throw new ArgumentException("Item Step has already been completed.");
			}

			return itemStep;
		}

		private async Task<bool> ShouldWaitForResponsibilityRuleToRun(WorkflowItemStep item, WorkflowItemStepSettingModel itemStep, long itemStepID, EventInfo eventInfo)
		{
			const int MAX_RETRIES = 3;
			const string RETRIES = "Retries";

			string assetSql = "left join Asset A on A.Object = I.Object and A.ObjectID = I.ObjectID";

			if (eventInfo.Object.Object == SystemObjects.Issue)
			{
				assetSql = @"left join Issue SS on SS.ID = I.ObjectID and I.Object = 'Issue'
							left join Asset A on A.Object = SS.Object and A.ObjectID = SS.ObjectID";
			}

			DateTime? createdOnDate = await QueryFirstOrDefaultAsync<DateTime?>($@"
					select  A.CreatedOn 
					from    workflow.ItemStep S
							inner join workflow.Item I on I.ID = S.ItemID
							{assetSql}
							inner join workflow.VersionStep VS on VS.ID = S.StepID
							inner join workflow.version V on V.ID = VS.VersionID
							inner join workflow.type T on T.PublishedVersionID = V.ID
							inner join workflow.EventRegistration E on E.TypeID = T.ID and E.ChangeType = @addChangeType
					where   S.ID = @itemStepID", new { itemStepID, addChangeType = (int)ChangeType.Add });

			if (createdOnDate.HasValue)
			{
				XElement root = XElement.Parse(item.Settings);
				int retries = 0;

				if (root.Attribute(RETRIES) == null)
				{
					root.Add(new XAttribute(RETRIES, 0));
				}
				else
				{
					if (!int.TryParse(root.Attribute(RETRIES).Value, out retries))
					{
						Console.WriteLine("ERROR - COULD NOT PARSE RETRIES VALUE");

						return false;
					}
				}

				if (retries > MAX_RETRIES)
				{
					return false;
				}
				else
				{
					DateTime? lastRunOn = await QueryFirstOrDefaultAsync<DateTime?>(@"select min(R.LastRunOn) from ResponsibilityType T
						left join ResponsibilityTypeRelationRule R on R.ResponsibilityTypeID = T.ID
						where T.ID = @ResponsibilityTypeID", new { itemStep.ResponsibilityTypeID });

					if (lastRunOn.HasValue)
					{
						if (lastRunOn < createdOnDate)
						{
							int duration = (int)Math.Pow(2, retries);
							retries++;
							root.Attribute(RETRIES).SetValue(retries);
							await QueueSource.CreateScheduledTopicMessageAsync(eventInfo, DateTimeOffset.UtcNow.AddMinutes(duration));
							item.Settings = root.ToString();
							await SaveChangesAsync();
							Console.WriteLine($"DEBUG DELAYING EXECUTION OF STEP {duration} MINUTE(S) UNTIL RESPONSIBILITY RULE HAS RUN (ATTEMPT {retries} OF {MAX_RETRIES})");
							
							return true;
						}
					}
				}
			}

			return false;
		}

		private async Task<string> GetWorkflowAssignedResponsibility(int typeId, int stepId, long itemId)
		{
			return await Database.Connection.QueryFirstOrDefaultAsync<string>("[utility].[GetAssignedResponsibilityNameForWorkflow] @id, @stepId, @itemId", new { id = typeId, stepId, itemId });
		}

		private string GetDisplayDateTimeValue(string type, string FormattedValue)
		{
			DateTime dateValue;
			string fieldValue;

			if (type == "Date")
			{
				if (DateTime.TryParseExact(FormattedValue, "M/d/yyyy h:mm:ss tt", CultureInfo.CurrentCulture, DateTimeStyles.None, out dateValue))
				{
					string formattedDate = dateValue.ToString("dd MMM yyyy");
					fieldValue = formattedDate;
				}
				else if (DateTime.TryParseExact(FormattedValue, "MM/dd/yyyy HH:mm:ss", null, DateTimeStyles.None, out dateValue))
				{
					string formattedDate = dateValue.ToString("dd MMM yyyy");
					fieldValue = formattedDate;
				}
				else if (DateTime.TryParseExact(FormattedValue, "M/d/yyyy", null, DateTimeStyles.None, out dateValue))
				{
					string formattedDate = dateValue.ToString("dd MMM yyyy");
					fieldValue = formattedDate;
				}
				else if (DateTime.TryParseExact(FormattedValue, "MM/dd/yyyy", null, DateTimeStyles.None, out dateValue))
				{
					string formattedDate = dateValue.ToString("dd MMM yyyy");
					fieldValue = formattedDate;
				}
				else
				{
					fieldValue = FormattedValue;
				}
			}
			else if (type == "DateTime")
			{
				if (DateTime.TryParse(FormattedValue, out dateValue))
				{
					string formattedDate = dateValue.ToString("dd MMM yyyy HH:mm:ss");
					fieldValue = formattedDate;
				}
				else
				{
					fieldValue = FormattedValue;
				}
			}
			else
			{
				fieldValue = FormattedValue;
			}

			return fieldValue;
		}

		#endregion
	}
}
