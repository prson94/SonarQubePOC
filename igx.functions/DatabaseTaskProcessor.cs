using Azure.Messaging.ServiceBus;
using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using d360.extensions.search;
using d360.model;
using d360.utils.company;
using Dapper;
using igx.functions.consumption.models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace igx.functions.databasetaskprocessor
{
	public class DatabaseTaskProcessor: BaseFunction
	{
        const int DEFAULT_QUEUE_ITEMS = 500;

		IMailProvider Mail;
		IQueueSource Queue;
		ElasticSearchSource Search;

		public DatabaseTaskProcessor(IConfiguration config, IMailProvider mail, IQueueSource queue, ElasticSearchSource search) : base(config)
		{
			Mail = mail;
			Queue = queue;
			Search = search;
		}

		[FunctionName("DatabaseTaskScheduler")]
        public async Task RunScheduler([TimerTrigger("*/1 * * * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
        {
            var companies = GetCompaniesByCurrentSlot();
            companies.ForEach(async company =>
            {
				var logProperties = new Dictionary<string, object> {
					{ "Function", "DatabaseTask_Scheduler" },
					{ "CompanyID", company.CompanyID },
					{ "UrlPrefix", company.UrlPrefix }
				};

				using (log.BeginScope(logProperties))
				{ 
					try
					{
						using (var outerCompanyConnection = new SqlConnection(CompanyConnectionUtils.GetConnectionString(company.CompanyID, company.Server, company.Username, company.Password)))
						{
							await outerCompanyConnection.OpenIfClosed();
							if (HasWork(outerCompanyConnection))
							{
								await Queue.CreateFilteredTopicMessageAsync(Config["EventBusTopicName"], new DatabaseProcessorTask(company));
							}
						}
					}
					catch (Exception ex)
					{
						log.LogError(ex, "Task Processor Failed for company.");
					}				
				}
            });
        }

        [FunctionName("DatabaseTaskProcessor")]
        public async Task RunProcessor([ServiceBusTrigger("%EventBusTopicName%", "DatabaseTask")] ServiceBusReceivedMessage brokeredMessage, ILogger log)
        {
			try
			{
				var messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
				var task = JsonConvert.DeserializeObject<DatabaseProcessorTask>(messageString);
				var company = task.Company;

				var logProperties = new Dictionary<string, object> {
					{ "Function", "DatabaseTask_Scheduler" },
					{ "CompanyID", company.CompanyID },
					{ "UrlPrefix", company.UrlPrefix }
				};

				using (log.BeginScope(logProperties))
				{
					try
					{
						var numberOfQueueItems = DEFAULT_QUEUE_ITEMS;
						if (int.TryParse(Config["TaskProcessorNumQueueItems"], out int tempNumQueueItems))
						{
							numberOfQueueItems = tempNumQueueItems > 0 ? tempNumQueueItems : DEFAULT_QUEUE_ITEMS;
						}

						var indexCollectionModel = new ObjectIndexCollectionModel();

						using (var outerCompanyConnection = new SqlConnection(CompanyConnectionUtils.GetConnectionString(company.CompanyID, company.Server, company.Username, company.Password)))
						{
							outerCompanyConnection.Open();

							if (!HasWork(outerCompanyConnection))
							{
								return;
							}

							var checkoutAndGetQueueItemSql = $@"
								declare @IDs table (ID uniqueidentifier,index ix_IDs clustered (ID))

								;WITH CTE AS 
								( 
									SELECT TOP {numberOfQueueItems} MachineAssigned, ID
									FROM [queue].[task]
									where MachineAssigned is null and NumberOfRetries < 2  and [date] < DATEADD(second, -30, getutcdate()) 
									ORDER BY [Date] ASC
								) 
								UPDATE CTE set MachineAssigned = @m OUTPUT deleted.ID into @IDs  

								select  T.* 
								from    [queue].[Task] T
										inner join @IDs S on S.ID = T.ID
								order by T.[Date]
								";

							List<QueueTask> queueItems = null;

							// Checkout select and update should be done in transaction to avoid other function instances from
							// checking out the same items.  
							using (var trans = outerCompanyConnection.BeginTransaction())
							{
								try
								{
									queueItems = outerCompanyConnection.Query<QueueTask>(
										checkoutAndGetQueueItemSql, 
										new { 
											m = new DbString { Value = Environment.MachineName, IsAnsi = true, Length = 250 } 
										}, 
										transaction: trans, 
										commandTimeout: 60
									).ToList();

									trans.Commit();
								}
								catch (Exception ex)
								{
									try
									{
										if (trans != null)
										{
											trans.Rollback();
										}
									}
									catch
									{
									}
									log.LogError(ex, "Error checking out queue items from table.");
								}
							}

							if (queueItems != null)
							{
								queueItems.ForEach(async q =>
								{
									try
									{
										using (var companyConnection = new SqlConnection(CompanyConnectionUtils.GetConnectionString(company.CompanyID, company.Server, company.Username, company.Password)))
										{
											await companyConnection.OpenIfClosed();

											switch (q.Action)
											{
												case "Add":
													addAuditEntry(companyConnection, "Created", q);
													resolveObjectObjectID(q, out var @object, out var objectId);
													resolveIndexItem(company, indexCollectionModel, companyConnection, @object, objectId, "A", q.AssetID);
													break;
												case "Delete":
													addAuditEntry(companyConnection, "Removed", q);
													resolveObjectObjectID(q, out @object, out objectId);
													resolveIndexItem(company, indexCollectionModel, companyConnection, @object, objectId, "D", q.AssetID);
													break;
												case "EventTopicNotification":
													bool parseSuccessful = true;
													if (!string.IsNullOrEmpty(q.Custom))
													{
														var customXml = XElement.Parse(q.Custom);
														d360.core.enums.Workflow.ChangeType ct;
														SystemObjects obj;
														SystemObjects objType;

														if (!Enum.TryParse(customXml.Element("ChangeType").Value, out ct)) { parseSuccessful = false; }
														if (!Enum.TryParse(customXml.Element("ObjectType").Value, out objType)) { parseSuccessful = false; }
														if (!Enum.TryParse(customXml.Element("Object").Value, out obj)) { parseSuccessful = false; }
														if (!Enum.TryParse(customXml.Element("ObjectTypeID").Value, out int objectTypeID)) { parseSuccessful = false; }

														if (parseSuccessful)
														{
															var topicName = company.EventTopic;
															Queue.CreateTopicMessage(topicName, new EventInfo
															{
																Action = ct,
																CompanyID = company.CompanyID,
																DomainPrefix = company.UrlPrefix,
																Object = new EventObjectInfo
																{
																	Object = obj,
																	ObjectID = q.ObjectID,
																	ObjectType = objType,
																	ObjectTypeID = objectTypeID
																},
																ResourceID = 0
															});
														}
													}

													if (!parseSuccessful)
													{
														throw new ApplicationException("XML field does not have any valid information contained within.");
													}
													break;
												case "Notify":
													if (q.Object == "TaggedComment")
													{
														var comment = companyConnection.Query<(int AssetID, DateTime? CommentDate)>(
															@"select AssetID, isNull(UpdatedOn, CreatedOn) as CommentDate from Comment where ID = @id", 
															new { id = q.ObjectID }, null, true, 900
														).FirstOrDefault();
															
														if (comment.AssetID > 0)
														{
															var notification = JsonConvert.DeserializeObject<CommentNotification>(q.Custom);
															if (notification != null)
															{
																var displayValue = companyConnection.Query<string>("Select DisplayValue from AssetDetail A where A.ID = @AssetID", new { AssetID = notification.CommentedOnAssetId ?? comment.AssetID }).FirstOrDefault();

																var rootUrl = $"https://{company.UrlPrefix}.data3sixty.com";

																string mailBody = $@"
																						<html>
																						<head>
																							<style>
																								body {{
																									margin-top: 20px;
																									margin-left: 50px;
																									margin-right: 50px;
																									font-family: Trebuchet MS, Arial, Helvetica, sans-serif;
																								}}
																								.header {{
																									font-weight: bold;
																									padding-bottom: 10px;
																								}}
																								.content {{
																									padding-bottom: 20px;
																									padding-top: 20px;
																									border-top: 2px solid #d7d8dc;
																									border-bottom: 2px solid #d7d8dc;
																								}}
																								.footer {{
																									padding-top: 10px;
																									text-align: right;
																								}}
																								.button {{
																									display: inline-flex;
																									position: relative;
																									flex-direction: row;
																									justify-content: center;
																									align-items: center;
																									flex-shrink: 0;
																									background: #006fba;
																									color: #ffffff;
																									border: none;
																									border-radius: 4px;
																									line-height: 200%;
																									height:32px;
																								}}
																								a {{
																									text-decoration: none;
																								}}
																								a:link .link {{
																									color: #006fba;
																								}}
																								a:hover .link {{
																									text-decoration: underline;
																								}}
																								a:visited .link {{
																									color: #006fba;
																								}}
																								img {{ border-style: none; }}
																							</style>
																						</head>
																						<body>
																							<div class='header'>
																								{string.Format(Notifications.TaggedCommentMailHeader, notification.CommenterName)}
																							</div>
																							<div class='content'>
																								{string.Format(Notifications.TaggedCommentMailBody, notification.CommenterName, rootUrl, notification.AssetUrl, displayValue, comment.CommentDate.Value.ToString("hh:mm tt 'UTC' 'on' dd MMM yyyy"))}                                                                                            
																							<br />
																							<br />
																							<a href='{rootUrl}{notification.CommentUrl}' class='button'>&nbsp;&nbsp;{Notifications.TaggedCommentMailCommentLink}&nbsp;&nbsp;</a>
																						</div>
																							<div class='footer'>
																								<img src ='{rootUrl}/Content/images/logo.mail.small.png' alt='D360 Govern' style='border-style:none;'> 
																							</div>
																						</body>
																						</html>                                                                                        
																						";
																Mail.SendMessage(Notifications.TaggedCommentMailSender, notification.Subject, notification.RecipientEmail, notification.RecipientName, mailBody, notification.IsHtml).Wait();
															}
														}
													}
													break;
												case "ObjectIndex":
													resolveIndexItem(company, indexCollectionModel, companyConnection, q.Object, q.ObjectID, q.Custom, q.AssetID);
													break;
												case "Update":
													addAuditEntry(companyConnection, "Updated", q);
													resolveObjectObjectID(q, out @object, out objectId);
													resolveIndexItem(company, indexCollectionModel, companyConnection, @object, objectId, "U", q.AssetID);
													break;
												case "TagConsolidated":
													addAuditEntry(companyConnection, "Tag Consolidate", q);
													break;
												case "CompanySettingsUpdate":
													addAuditEntry(companyConnection, "Update settings", q);
													break;
												case "QueueRebuild":
													if (!string.IsNullOrEmpty(q.Custom))
													{
														switch (q.Custom)
														{
															case "AssetGraph":
																Queue.CreateMessage(Config["AssetGraphQueue"], new RebuildAssetGraphModel { CompanyID = company.CompanyID });
																break;
															case "DisplayValue":
																Queue.CreateMessage(Config["DisplayValueQueue"], new DisplayUpdateInfo { CompanyID = company.CompanyID, RebuildAll = true });
																break;
															case "SearchIndex":
																ReindexModel model = new ReindexModel { CompanyID = company.CompanyID };
																if (!string.IsNullOrEmpty(q.Object) && SearchIndexer.IsIndexable(q.Object))
																{
																	model.Category = q.Object;
																}
																Queue.CreateMessage(Config["SearchIndexQueue"], model);
																break;
														}
													}
													break;
											}
											
											companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
										}
									}
									catch (Exception ex)
									{
										log.LogError(ex, "Error processing queue item.");
									}
								});
							}
						}

						#region Now deal with INDEXING

						try
						{
							if (indexCollectionModel.Adds.Count > 0)
							{
								Search.AddToIndex(indexCollectionModel.Adds);
							}

							if (indexCollectionModel.Deletes.Count > 0)
							{
								Search.RemoveFromIndex(indexCollectionModel.Deletes);
							}

							if (indexCollectionModel.Updates.Count > 0)
							{
								Search.UpdateInIndex(indexCollectionModel.Updates);
							}
							
							if (indexCollectionModel.ContainsIndexerCollections())
							{
								using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(company.CompanyID, Config["CommunityContext"]))
								{
									companyConnection.Open();
									var indexer = new SearchIndexer(companyConnection, company.CompanyID, Search);

									if (indexCollectionModel.UpsertByUid.Any())
									{
										indexer.IndexAssets(indexCollectionModel.UpsertByUid);
									}

									if (indexCollectionModel.UpsertByObject.Any())
									{
										indexer.IndexAssets(indexCollectionModel.UpsertByObject);
									}

									if (indexCollectionModel.UpsertPathByAssetId.Any())
									{
										indexer.IndexUpdateAssetPaths(indexCollectionModel.UpsertPathByAssetId);
									}

									indexer = null;
								}
							}
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Failed processing queue message.");
						}

						#endregion
					}
					catch (Exception ex)
					{
						log.LogError(ex, "Task Processor Failed for company.");
					}
				}
			}
			catch (Exception ex)
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", "DatabaseTask_Process" }
				};
				using (log.BeginScope(logProperties))
				{
					log.LogCritical(ex, "Critical error in task queue processor.");
				}
			}
        }

		#region Utility Methods

		private void resolveObjectObjectID(QueueTask queueRecord, out string @object, out int objectId)
		{
			if(queueRecord.Object == "ResponsibilityTypeRelationOverrideItem" && !string.IsNullOrEmpty(queueRecord.Custom) && queueRecord.Custom.Contains("<ActionObjectID>"))
			{
				var customXml = XElement.Parse(queueRecord.Custom);
				@object = customXml.Element("ActionObject").Value;
				objectId = int.Parse(customXml.Element("ActionObjectID").Value);
			} else {
				@object = queueRecord.Object;
				objectId = queueRecord.ObjectID;
			} 
		}

		private string resolveIndexItem(CompanyWithDatabaseServerSettings company, ObjectIndexCollectionModel indexCollectionModel, SqlConnection companyConnection, string @object, int objectId, string action, long assetId)
        {
            if (!SearchIndexer.IsIndexable(@object)) 
            { 
                return string.Empty;
            }

            if (action == "Path")
            {
                indexCollectionModel.UpsertPathByAssetId.Add(assetId);
            }
            else if (action == "D") //Delete - asset is no longer present, so we can only use given parameters
            {
                IndexObjectModel indexObject = new IndexObjectModel
                {
                    CompanyID = company.CompanyID,
                    Category = SearchIndexer.GetCategoryFromObject(@object),
                    ID = objectId,
                    To = QueueAction.RemoveFromIndex,
                    RelativeUrl = "#"
                };

                if (assetId > 0)
                {
                    indexObject.AssetID = assetId;
                    indexObject.ItemUniqueID = assetId.ToString();
                }
                //Set uniqueID for index object
                if (@object == "Synonym")
                {
                    indexObject.ItemUniqueID = $"custom|{objectId}";
                }
                //Intersects have two search documents, se we need to delete both
                else if (@object == "Intersect")
                {
                    indexObject.Category = "Synonym";
                    indexObject.AssetType = "Synonym";

                    IndexObjectModel reciprocal = indexObject.ShallowCopy();
                    reciprocal.ItemUniqueID = $"intersect|{objectId}|O";
                    indexObject.ItemUniqueID = $"intersect|{objectId}|S";
                    indexCollectionModel.Deletes.Add(reciprocal);
                }

                indexCollectionModel.Deletes.Add(indexObject);
            }
            else //Add or update
            {
                if (@object == "Synonym" || @object == "ReferenceItemType")
                {
                    //These objects are not assets, so they do not have an Asset UID
                    indexCollectionModel.UpsertByObject.Add(new Tuple<string, long>(@object, objectId));
                }
                else if (@object == "Intersect" && assetId > 0)
                {
                    //Intersects of Predicate type 6 are synonyms and are indexed
                    bool isSynonym = companyConnection.Query<bool>(@"SELECT COUNT(1)
                                        FROM [dbo].[Intersect] i
                                        WHERE EXISTS (SELECT 1 FROM [dbo].[IntersectType] it
                                            INNER JOIN [dbo].[Predicate] p ON it.PredicateID = p.id
                                            WHERE p.type = 6 AND i.IntersectTypeID = it.ID)
                                        AND i.id = @a", new { a = objectId }).SingleOrDefault();

                    if (isSynonym)
                    {
                        indexCollectionModel.UpsertByObject.Add(new Tuple<string, long>(@object, objectId));
                    }
                }
                else
                {
                    Guid AssetUid = (assetId > 0) ?
                        companyConnection.Query<Guid>("SELECT Uid FROM [dbo].[Asset] WHERE id = @a", new { a = assetId }).SingleOrDefault() :
                        companyConnection.Query<Guid>("SELECT Uid FROM [dbo].[Asset] WHERE [Object] = @t AND [ObjectID] = @i", new { t = @object, i = objectId }).SingleOrDefault();

                    if (AssetUid != Guid.Empty)
                    {
                        indexCollectionModel.UpsertByUid.Add(AssetUid);
                    }
                }
            }

            return string.Empty;
        }

        private void addAuditEntry(SqlConnection companyConnection, string oper, QueueTask queueRecord)
        {
            if (!string.IsNullOrEmpty(queueRecord.Custom))
            {
                AuditCustomDataModel model = null;

                if (queueRecord.Custom.Contains("<ActionObjectID>"))
                {
                    // Treat as XML.
                    var customXml = XElement.Parse(queueRecord.Custom);
                    model = new AuditCustomDataModel
                    {
                        ActionObject = customXml.Element("ActionObject").Value,
                        ActionObjectID = int.Parse(customXml.Element("ActionObjectID").Value),
                        ActionObjectValue = (customXml.Element("ActionObjectValue") == null ? null : customXml.Element("ActionObjectValue").Value),
                        ResourceID = int.Parse(customXml.Element("ResourceID").Value),
                        Fields = new List<AuditCustomDataFieldModel>()
                    };
                    if (customXml.Element("FieldInfo") != null)
                    {
                        foreach (var f in customXml.Element("FieldInfo").Elements())
                        {
                            model.Fields.Add(new AuditCustomDataFieldModel
                            {
                                FieldTypeID = int.Parse(f.Element("FieldTypeID") != null ? f.Element("FieldTypeID").Value : "0"),
                                Name = (string)f.Element("Name") ?? "",
                                Value = (string)f.Element("Value") ?? ""
                            });
                        }
                    }
                }
                else
                {
                    // Treat as JSON.
                    model = JsonConvert.DeserializeObject<AuditCustomDataModel>(queueRecord.Custom);
                }

                if (model != null)
                {
                    var parameters = new DynamicParameters();

                    parameters.Add("@MainObject", queueRecord.Object, DbType.AnsiString, size: 50);
                    parameters.Add("@MainObjectID", queueRecord.ObjectID);
                    parameters.Add("@DependentObject", model.ActionObject, DbType.AnsiString, size: 50);
                    parameters.Add("@DependentObjectID", model.ActionObjectID);
                    parameters.Add("@Date", queueRecord.Date);
                    parameters.Add("@ResourceID", model.ResourceID);
                    parameters.Add("@Action", oper, DbType.AnsiString, size: 15);
                    parameters.Add("@NewValue", model.ActionObjectValue, DbType.AnsiString, size: 50);

                    if (model.Fields != null && model.Fields.Count > 0)
                    {
                        parameters.Add("@AuditFieldTable", getFieldsTable(model).AsTableValuedParameter("[dbo].[AuditFieldTable]"));
                    }

                    companyConnection.Query(
                        "[utility].[AddAuditEntry]",
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 600
                        );
                }
            }
        }

        private DataTable getFieldsTable(AuditCustomDataModel model)
        {
            var tb = new DataTable();

            tb.Columns.Add("FieldTypeID", typeof(int));
            tb.Columns.Add("FieldName", typeof(string));
            tb.Columns.Add("Value", typeof(string));

            foreach (var f in model.Fields)
            {
                var fieldRow = tb.NewRow();

                fieldRow["FieldName"] = f.Name;
                fieldRow["FieldTypeID"] = f.FieldTypeID;
                fieldRow["Value"] = f.Value;

                tb.Rows.Add(fieldRow);
            }
            return tb;
        }

        private bool HasWork(SqlConnection conn)
        {
            var existsSql = @"IF EXISTS (SELECT 1 FROM [queue].task where MachineAssigned is null and NumberOfRetries < 2)
                                                BEGIN
                                                    select 1;
                                                END
                                                ELSE
                                                BEGIN
                                                   select 0;
                                                END";
			return conn.QuerySingle<bool>(existsSql);
        }

		#endregion
	}
}
