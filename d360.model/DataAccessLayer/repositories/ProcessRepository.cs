using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.entities.Process;
using d360.core.enums;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;

using Dapper;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SpreadsheetLight;
using SpreadsheetLight.Drawing;

namespace d360.model.DataAccessLayer
{
	public class ProcessRepository : BaseRepository, IProcessRepository
	{
		internal ICompanyContext Company;
		internal IAssetRepository AssetRepository;
		internal IStorageProvider StorageProvider;

		public ProcessRepository(ICompanyContext context, IAssetRepository assetRepository, IStorageProvider storage) : base(context)
		{
			Company = context;
			AssetRepository = assetRepository;
			StorageProvider = storage;
		}

		public async Task<IEnumerable<dynamic>> GetAvailableDiagramNodesForAsset(Guid assetUid)
		{
			var sql = $@"
						declare @assetTypeUid uniqueidentifier = (select at.uid from asset a 
							inner join assettype at on a.AssetTypeID = at.ID
						where a.uid = @assetUid)

						SELECT     A.[Name]
									,ISNULL(A.[Description],'') as Description
									,A.[Class] as ClassID
									,A.[uid]
									,A.DisplayFormat
									,A.FlowObjectType
									,P.[Path]
									,AT.Icon as Icon
						FROM        AssetType A
									cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
									left join [dbo].[AssetTypeStyle] AT on (A.ID = AT.ID)
									inner join IntersectTypeDetail itd on itd.ObjectUid = a.uid and itd.SubjectUid = @assetTypeUid and itd.predicateType = @predicateType
						where       
						A.[State] = 1 and A.ObjectID != 0 and Class = 15
						order by Name ";

			var nodes = await Company.QueryAsync<dynamic>(sql, new { assetUid, predicateType = (int)PredicateType.Diagram }, ApiTimeout);
			return nodes;
		}

		public ProcessDiagramModel GetAssetsProcessDiagram(Guid assetUid)
		{
			var targetAsset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);

			var diagramJson = string.Empty;

			if (targetAsset != null)
			{
				var diagram = Company.AssetProcessDiagrams.FirstOrDefault(x => x.AssetId == targetAsset.ID);
				
				if (diagram != null && !string.IsNullOrEmpty(diagram.Diagram))
				{
					diagramJson = diagram.Diagram;
				}

			}
			var model = new ProcessDiagramModel();

			if (string.IsNullOrEmpty(diagramJson))
			{
				model.nodeDataArray = new List<NodeData>();
				model.linkDataArray = new List<LinkData>();
			}
			else
			{
				model = JsonConvert.DeserializeObject<ProcessDiagramModel>(diagramJson);
			}

			List<Guid> assetUids = model.nodeDataArray.Select(x => x.AssetUid).ToList();

			//expand model with db data
			var nodesExpandedData = Company.Query<dynamic>(@"select 
					a.uid,
					ATS.Icon as icon,
					a.objectId,
					case at.FlowObjectType
						when 1 then 'event'
						when 2 then 'activity'
						when 3 then 'gateway'
					end as category,
					'#708EA6' as refItemColor,
					AT.uid as assetTypeUid,
					AT.name as assetTypeName,
					field.json as fields
					from Asset A
						inner join AssetType AT on AT.ID = A.AssetTypeID
						left join AssetTypeStyle ATS on ATS.Id = AT.Id
						cross apply(
						select * from (
							select ft.Name, f.Value from Field f 
							inner join FieldType ft on f.FieldTypeID = ft.ID and ft.Type <> 'Tag'
							where assetid = a.id
						) as Fields
						for json path
						)field(json)
					where A.uid in @assetUids", new { assetUids }, ApiTimeout).ToList();

			var badges = GetDiagramAssetBadges(assetUid);

			foreach (var item in nodesExpandedData)
			{
				var json = JsonConvert.SerializeObject(item);
				var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
				var node = model.nodeDataArray.FirstOrDefault(x => x.AssetUid == Guid.Parse(dictionary["uid"]));

				var badge = badges.FirstOrDefault(x => x.AssetUid == node.AssetUid);

				if (badge != null)
				{
					node["relCount"] = badge.RelationshipCount.ToString();
				}
				else
				{
					node["relCount"] = "0";
				}

				node["icon"] = dictionary["icon"];
				node["category"] = dictionary["category"];
				node["refItemColor"] = dictionary["refItemColor"];
				node["assetTypeUid"] = dictionary["assetTypeUid"];
				node["assetTypeName"] = dictionary["assetTypeName"];
				node["objectId"] = dictionary["objectId"];

				if (dictionary["fields"] != null)
				{
					var arr = JsonConvert.DeserializeObject<JArray>(dictionary["fields"], new JsonSerializerSettings()
					{
						DateParseHandling = DateParseHandling.None
					});
					foreach (JObject field in arr)
					{
						node[field["Name"].ToString()] = field["Value"].ToString();
					}
				}
			}

			var linksExpandedData = Company.Query<dynamic>(@"declare @diagram nvarchar(max) = (
				select apd.Diagram  as json from asset a 
					inner join AssetProcessDiagram apd on apd.AssetID = a.ID
				where a.uid = @assetUid)

				;with links as (
				SELECT  
					JSON_VALUE(nda.value, '$.from') AS FromUid,
					JSON_VALUE(nda.value, '$.to') AS ToUid,
					JSON_VALUE(nda.value, '$.labelUid') AS LabelUid
				FROM OPENJSON(@diagram, '$.linkDataArray') as nda)
				select links.*, CL.Value from links
				 inner join ConnectorLabel CL on CL.uid = links.labeluid and CL.State <> 3
				where labeluid is not null", new { assetUid }, ApiTimeout).ToList();

			foreach (var item in linksExpandedData)
			{
				var json = JsonConvert.SerializeObject(item);
				var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

				Guid fromUid = Guid.Parse(dict["FromUid"]);
				Guid toUid = Guid.Parse(dict["ToUid"]);
				Guid labelUid = Guid.Parse(dict["LabelUid"]);
				string labelValue = dict["Value"];

				var link = model.linkDataArray.FirstOrDefault(x => x.from == fromUid && x.to == toUid && x.labelUid == labelUid);
				
				if (link != null)
				{
					link.label = labelValue;
				}
			}

			model.linkToPortIdProperty = "toPort";
			model.linkFromPortIdProperty = "fromPort";

			//handle invalid data
			foreach (var item in model.nodeDataArray.Where(x => !x.ContainsKey("assetTypeUid")))
			{
				item.Add("isInvalid", "true");
				item.Add("Name", "An administrator removed this diagram asset type.");
				item.Add("icon", "fa-exclamation-triangle");
				item.Add("category", "deleted-node");
			}

			model.nodeDataArray = model.nodeDataArray.OrderBy(x => x.StepNo).ToList();

			return model;
		}

		public List<ValidationError> UpdateProcessDiagram(ApiExecution execution, ProcessDiagramModel model,
			List<NodeData> toAdd, List<NodeData> toUpdate, List<NodeData> toDelete, long targetAssetId,
			bool isDiagramReplace, List<ProcessDiagramCopyRelationshipModel> copyRelationshipModel,
			List<ProcessDiagramCopyMapper> pdCopyMapper)
		{
			var validationRes = new List<ValidationError>();

			if (Company.Database.Connection.State != ConnectionState.Open)
			{
				Company.Connection.Open();
			}

			//Validation passed lets do some work
			var totalCount = toAdd.Count + toDelete.Count + toUpdate.Count;

			List<Guid> addedAssets = new List<Guid>();

			Company.Add(execution);
			Company.SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var assetsTable = "api.ExecutionDiagramAsset";
			var fieldsTable = "api.ExecutionDiagramAssetField";

			//copy data to tables
			var fieldTable = new DataTable();
			fieldTable.Columns.Add(new DataColumn("ExecutionID", typeof(Guid)));
			fieldTable.Columns.Add(new DataColumn("ExecutionItemUid", typeof(Guid)));
			fieldTable.Columns.Add(new DataColumn("FieldName", typeof(string)));
			fieldTable.Columns.Add(new DataColumn("FieldValue", typeof(string)));
			fieldTable.Columns.Add(new DataColumn("FormattedValue", typeof(string)));


			//bulk copy data to temporary tables
			var assetTable = new DataTable();
			assetTable.Columns.Add(new DataColumn("ExecutionID", typeof(Guid)));
			assetTable.Columns.Add(new DataColumn("ExecutionItemUid", typeof(Guid)));
			assetTable.Columns.Add(new DataColumn("Uid", typeof(Guid)));
			assetTable.Columns.Add(new DataColumn("AssetTypeUid", typeof(Guid)));
			assetTable.Columns.Add(new DataColumn("Action", typeof(string)));

			foreach (var item in toAdd)
			{
				var executionUid = Guid.NewGuid();
				var row = assetTable.NewRow();
				row["ExecutionID"] = execution.ExecutionID;
				row["ExecutionItemUid"] = item.AssetUid;
				row["Uid"] = DBNull.Value;
				row["AssetTypeUid"] = item.AssetTypeUid;
				row["Action"] = "Insert";
				assetTable.Rows.Add(row);

				foreach (var field in item.CustomFields.Where(x => !string.IsNullOrEmpty(x.Value)))
				{
					var fieldRow = fieldTable.NewRow();
					fieldRow["ExecutionID"] = execution.ExecutionID;
					fieldRow["ExecutionItemUid"] = item.AssetUid;
					fieldRow["FieldName"] = field.Key.ToString();
					
					if (field.Value == null)
					{
						fieldRow["FieldValue"] = DBNull.Value;
						fieldRow["FormattedValue"] = DBNull.Value;

					}
					else
					{
						fieldRow["FieldValue"] = field.Value.ToString();
						fieldRow["FormattedValue"] = field.Value.ToString();

					}
					fieldTable.Rows.Add(fieldRow);
				}
			}

			foreach (var item in toUpdate)
			{
				var executionUid = Guid.NewGuid();
				var row = assetTable.NewRow();
				row["ExecutionID"] = execution.ExecutionID;
				row["ExecutionItemUid"] = item.AssetUid;
				row["Uid"] = item.AssetUid;
				row["AssetTypeUid"] = item.AssetTypeUid;
				row["Action"] = "Update";
				assetTable.Rows.Add(row);

				foreach (var field in item.CustomFields.Where(x => x.Value != null))
				{
					var fieldRow = fieldTable.NewRow();
					fieldRow["ExecutionID"] = execution.ExecutionID;
					fieldRow["ExecutionItemUid"] = item.AssetUid;
					fieldRow["FieldName"] = field.Key.ToString();
					
					if (field.Value == null)
					{
						fieldRow["FieldValue"] = DBNull.Value;
						fieldRow["FormattedValue"] = DBNull.Value;

					}
					else
					{
						fieldRow["FieldValue"] = field.Value.ToString();
						fieldRow["FormattedValue"] = field.Value.ToString();

					}
					fieldTable.Rows.Add(fieldRow);
				}
			}

			foreach (var item in toDelete)
			{
				var row = assetTable.NewRow();
				row["ExecutionID"] = execution.ExecutionID;
				row["ExecutionItemUid"] = item.AssetUid;
				row["Uid"] = item.AssetUid;
				row["AssetTypeUid"] = item.AssetTypeUid;
				row["Action"] = "Delete";
				assetTable.Rows.Add(row);
			}


			var conn = Company.Connection;

			using (var bulk = new SqlBulkCopy(conn))
			{
				bulk.BulkCopyTimeout = 0;
				bulk.DestinationTableName = assetsTable;
				bulk.WriteToServer(assetTable);
			}

			using (var bulk = new SqlBulkCopy(conn))
			{
				bulk.BulkCopyTimeout = 0;
				bulk.DestinationTableName = fieldsTable;
				bulk.WriteToServer(fieldTable);
			}

			using (var trans = Company.Connection.BeginTransaction())
			{
				try
				{
					//delete assets
					conn.Execute($@"
									delete F
									from Field F
										inner join api.executiondiagramasset S on S.Action = 'Delete'
										inner join asset a on s.uid = a.uid
										inner join [Intersect] I on i.object = a.object and i.objectid = a.objectid
										where s.executionid = @ExecutionID and f.objecttype = 'Intersect' and f.objectid = I.Id

									delete F
									from Field F
										inner join api.executiondiagramasset S on S.Action = 'Delete'
										inner join asset a on s.uid = a.uid
										inner join [Intersect] I on i.subject = a.object and i.subjectid = a.objectid
										where s.executionid = @ExecutionID and f.objecttype = 'Intersect' and f.objectid = I.Id

									 delete	T
										from	[Intersect] T
												inner join api.executiondiagramasset S on S.Action = 'Delete'
												inner join asset a on s.uid = a.uid
										where s.executionid = @ExecutionID and T.object = a.object and T.objectid = a.objectid
									delete	T
										from	[Intersect] T
												inner join api.executiondiagramasset S on S.Action = 'Delete'
												inner join asset a on s.uid = a.uid
										where s.executionid = @ExecutionID and T.subject = a.object and T.subjectid = a.objectid;

									delete F
									from Field F
										inner join api.executiondiagramasset S on S.Action = 'Delete'
										inner join asset a on s.uid = a.uid
										where s.executionid = @ExecutionID and f.assetid = a.id

									delete Asset where Uid in (select S.Uid from api.executiondiagramasset S where S.Action = 'Delete' and s.executionid = @ExecutionID)", 
						new { execution.ExecutionID }, 
						transaction: trans);


					//add or update assets
					conn.Execute($@"
								update api.ExecutionDiagramAssetField
								set FieldTypeId = ft.id
								from 
									api.ExecutionDiagramAssetField as fields
									inner join api.ExecutionDiagramAsset eda on eda.ExecutionItemUid = fields.ExecutionItemUid
									inner join AssetType at on at.uid = eda.assettypeuid
									inner join FieldType ft on ft.assettypeid = at.id and ft.name = fields.fieldname
									where eda.executionid = @executionId

								drop table if exists #updatedAssets
								create table #updatedAssets(
								ExecutionItemUid uniqueidentifier,
								Uid uniqueidentifier
								)

								 merge   [Asset] as T
									using   (
										select eda.uid, 
										   eda.ExecutionItemUid, 
											at.id as assettypeid 
										from api.ExecutionDiagramAsset eda
												inner join AssetType AT on at.uid = eda.assettypeuid
										where eda.executionid = @executionid and eda.Action <> 'Delete'
											) S
									on S.Uid = T.Uid
									when matched
										then update set updatedby=@resourceId, updatedon=getutcdate()
									when    not matched then
										insert  (AssetTypeID,State,[Object], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
										values  (s.assettypeid,1,'Task', @resourceId, getutcdate(), @resourceId, getutcdate())
										output  S.ExecutionItemUid, inserted.uid into #updatedAssets;

								update api.ExecutionDiagramAsset
								set Uid = ca.Uid
								from #updatedAssets ca
								where ca.ExecutionItemUid = api.ExecutionDiagramAsset.ExecutionItemUid and api.ExecutionDiagramAsset.ExecutionID = @executionId

								update api.ExecutionDiagramAssetField
								 set Object = a.Object, 
								  ObjectID = a.ObjectID
								 from api.ExecutionDiagramAsset ca
								  inner join asset a on a.uid = ca.Uid
								 where ca.uid is not null 
								 and ca.ExecutionItemUid = api.ExecutionDiagramAssetField.ExecutionItemUid
								 and ca.ExecutionId = @executionId
 
								 update api.ExecutionDiagramAssetField
									set Object = a.Object, 
										ObjectID = a.ObjectID
									from #updatedAssets ca
										inner join asset a on a.uid = ca.Uid
									where ca.ExecutionItemUid = api.ExecutionDiagramAssetField.ExecutionItemUid

								merge Field as T
									using (select edaf.*,a.id as AssetId from api.ExecutionDiagramAssetField  edaf
										inner join api.ExecutionDiagramAsset eda on eda.executionitemuid = edaf.executionitemuid 
										inner join asset a on eda.uid = a.uid
										inner join fieldtype ft on ft.id = edaf.FieldTypeID
										where eda.executionid = @executionid and edaf.executionid = @executionid and ft.type <> 'Tag'
									) as S
									on (T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.Object and T.ObjectID = S.ObjectID)
									when matched and T.Value <> S.FieldValue COLLATE SQL_Latin1_General_CP1_CS_AS OR T.FormattedValue <> S.FormattedValue COLLATE SQL_Latin1_General_CP1_CS_AS 
									then update 
										set T.Value = S.FieldValue,
										T.FormattedValue = S.FormattedValue, 
										T.UpdatedBy = @resourceId, 
										T.UpdatedOn = getutcdate()
									when		not matched by target then
									insert		(AssetId,FieldTypeID, ObjectType, ObjectID, Value, FormattedValue, UpdatedBy, UpdatedOn)
									values		(S.AssetId,S.FieldTypeID, S.Object, S.ObjectID, S.FieldValue, S.FormattedValue, @resourceId, getutcdate());

								merge       AssetDisplayValue as T
								using       (
												select  A.Id as ID,
														ADV.DisplayValue,
														CONVERT(NVARCHAR(32), HashBytes('SHA1', ADV.DisplayValue), 2) as DisplayValueHash,
														SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix
												from    api.ExecutionDiagramAsset EDA
														inner join Asset A on a.uid = EDA.uid
														cross apply GetAssetDisplayValueByID(A.Id) ADV
												where   EDA.ExecutionID = @executionID 
														and EDA.uid is not null
														and ADV.DisplayValue is not null
											) as S 
								on          ( T.AssetID = S.ID )
								when		matched then
								update		set
												T.DisplayValue = S.DisplayValue,
												T.DisplayValueHash = S.DisplayValueHash,
												T.[DisplayValuePrefix] = S.DisplayValuePrefix,
												T.UpdatedOn = getutcdate()
								when		not matched by target then
								insert		(AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
								values		(S.ID, S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, getutcdate());", 
					new { executionId = execution.ExecutionID, resourceId = Company.CurrentResourceID },
					transaction: trans);

					if (isDiagramReplace)
					{
						//Copy relationship from source asset to target asset
						if (copyRelationshipModel.Count > 0)
						{
							CopyRelationships(execution, copyRelationshipModel, conn, trans);
						}

						if (pdCopyMapper.Count > 0)
						{
							CopyTags(execution, pdCopyMapper, conn, trans);
						}

					}

					var reader = conn.ExecuteReader($"select executionitemuid, uid from {assetsTable} where executionid = @ExecutionID and action <> 'Delete'", new { execution.ExecutionID }, transaction: trans);
					int updatedItemsCount = 0;
					
					while (reader.Read())
					{
						updatedItemsCount++;
						var executionUid = Guid.Parse(reader[0].ToString());
						var updatedAssetUid = Guid.Parse(reader[1].ToString());

						if (executionUid != updatedAssetUid)
						{
							var addedItem = model.nodeDataArray.FirstOrDefault(x => x.AssetUid == executionUid);
							if (addedItem == null)
							{
								throw new Exception("Added item missing from database results!");
							}
							addedItem.UpdateAssetUid(updatedAssetUid);
							addedAssets.Add(updatedAssetUid);

							foreach (var item in model.linkDataArray)
							{
								if (item.from == executionUid)
								{
									item.from = updatedAssetUid;
								}

								if (item.to == executionUid)
								{
									item.to = updatedAssetUid;
								}
							}
						}
					}

					reader.Close();

					if (updatedItemsCount != (toAdd.Count + toUpdate.Count))
					{
						throw new Exception("Count of updates do not match to database results!");
					}

					//simplify model for saving
					var simpleModel = new ProcessDiagramModel
					{
						@class = "ProcessDiagram",
						linkDataArray = model.linkDataArray,
						linkFromPortIdProperty = model.linkFromPortIdProperty,
						linkToPortIdProperty = model.linkToPortIdProperty,

						nodeDataArray = new List<NodeData>()
					};
					foreach (var node in model.nodeDataArray)
					{
						var simpleNode = new NodeData
						{
							{ "key", node.AssetUid.ToString() },
							{ "loc", node["loc"] }
						};
						simpleModel.nodeDataArray.Add(simpleNode);
					}

					conn.Execute($@"
									merge AssetProcessDiagram APD
									using(
										select @assetId as AssetId, @diagram as Diagram
									) as S
									on APD.AssetId = S.AssetId
									when matched
									then update
										set APD.Diagram = S.Diagram,
										APD.UpdatedBy = @resourceId,
										APD.UpdatedOn = getutcdate()
									when		not matched by target then
									insert		(AssetId,Diagram,CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values		(S.AssetId,S.Diagram, @resourceId, getutcdate(), @resourceId, getutcdate());",
									new
									{
										assetId = targetAssetId,
										diagram = (simpleModel.nodeDataArray.Count > 0 || simpleModel.linkDataArray.Count > 0) ? JsonConvert.SerializeObject(simpleModel) : null,
										resourceId = Company.CurrentResourceID
									}, transaction: trans);

					trans.Commit();

					execution.Processed = totalCount;
					execution.Error = 0;
					execution.CompletedOn = DateTime.UtcNow;
					Company.Update(execution);
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

					string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = message;
					execution.CompletedOn = DateTime.UtcNow;
					Company.Update(execution);
					validationRes.Add(new ValidationError() { Error = ex.Message });
				}
			}

			List<DatabaseBulkAssetResult> assetResults = new List<DatabaseBulkAssetResult>();
			List<DatabaseBulkAssetResult> intersectResults = new List<DatabaseBulkAssetResult>();

			//All added, updated and deleted nodes needs graph events
			assetResults.AddRange(addedAssets.Select(uid => new DatabaseBulkAssetResult()
			{
				Success = true,
				uid = uid,
				Object = "Diagram"
			}));

			assetResults.AddRange(toUpdate.Select(a => new DatabaseBulkAssetResult()
			{
				Success = true,
				uid = a.AssetUid,
				Object = "Diagram"
			}));

			assetResults.AddRange(toDelete.Select(delAsset => new DatabaseBulkAssetResult()
			{
				Success = true,
				uid = delAsset.AssetUid,
				Object = "Diagram"
			}));

			if (isDiagramReplace && addedAssets.Count > 0)
			{
				var intersectUids = Company.Query<Guid>(@"
						select i.uid from [Intersect] I 
							inner join Asset A on A.Object = I.Object and A.ObjectId = I.ObjectId
						where A.uid in @assets
						union
						select i.uid from [Intersect] I 
							inner join Asset A on A.Object = I.Subject and A.ObjectId = I.SubjectId
						where A.uid in @assets
				", new { assets = addedAssets }).ToList();

				intersectUids.ForEach(iuid =>
				{
					intersectResults.Add(new DatabaseBulkAssetResult()
					{
						Success = true,
						uid = iuid,
						Object = "Intersect"
					});
				});
			}

			try
			{
				if (assetResults.Any())
				{
					Company.SendAssetGraphEvents(assetResults);
				}
				if (intersectResults.Any())
				{
					Company.SendAssetGraphEvents(intersectResults, null, true);
				}
			}
			catch
			{

			}

			return validationRes;
		}

		private void CopyRelationships(ApiExecution execution, List<ProcessDiagramCopyRelationshipModel> copyRelationshipModel, SqlConnection conn, SqlTransaction trans)
		{
			var relJson = JsonConvert.SerializeObject(copyRelationshipModel);
			conn.Execute($@"
							drop table if exists #relationshipMap
							create table #relationshipMap(
								keyUid uniqueidentifier,
								intersectid int,
								[location] nvarchar(100)
							)

							drop table if exists #intersectMap
							create table #intersectMap(
								intersectFromId int,
								intersectToId int
							)

							insert into #relationshipMap
							select * from OPENJSON(@json)
							with (	keyUid uniqueidentifier '$.keyUid',
									intersectId int '$.IntersectId',
									location nvarchar(100) '$.Location')


							merge [Intersect] IT
							using(
								select I.IntersectTypeId,
										A.Object as Subject, 
										A.ObjectID as SubjectID,
										I.Object,
										I.ObjectID,
										I.Id as OldIntersectId
								from #relationshipMap
									inner join api.ExecutionDiagramAsset eda on eda.executionitemuid = #relationshipMap.keyuid
									inner join Asset A on a.uid = eda.uid
									inner join [Intersect] I on I.ID = #relationshipMap.intersectid
									where eda.executionid = @executionid and eda.Action <> 'Delete' and #relationshipMap.Location = 'Subject'
								union
								select I.IntersectTypeId,
										A.Object, 
										A.ObjectID,
										I.Object as Subject,
										I.ObjectID as SubjectID,
										I.Id as OldIntersectId
								from #relationshipMap
									inner join api.ExecutionDiagramAsset eda on eda.executionitemuid = #relationshipMap.keyuid
									inner join Asset A on a.uid = eda.uid
									inner join [Intersect] I on I.ID = #relationshipMap.intersectid
									where eda.executionid = @executionid and eda.Action <> 'Delete' and #relationshipMap.Location = 'Object'
							) src on (1=0)
							WHEN NOT MATCHED THEN INSERT (IntersectTypeId,Subject,SubjectID,Object,ObjectID,State,CreatedBy,CreatedOn,UpdatedBy,UpdatedOn,Owner,Deleted,Visible,uid)
							VALUES (	src.IntersectTypeId,
										src.Subject, 
										src.SubjectID,
										src.Object,
										src.ObjectID,
										'1',
										@resourceid,
										getutcdate(),
										@resourceid,
										getutcdate(),
										'BULK_API',
										0,
										1,
										newid())
							output src.OldIntersectId, Inserted.Id
							into #intersectMap;

							
							insert into Field (ObjectType,ObjectID,FieldTypeID,Value,FormattedValue,UpdatedBy,UpdatedOn)
							select 'Intersect', IM.intersectToId, F.FieldTypeId, F.Value, F.FormattedValue, @resourceId,getutcdate() 
							from #intersectMap IM
								inner join Field F on F.ObjectType = 'Intersect' and F.ObjectID = IM.intersectFromId

							", new { execution.ExecutionID, resourceId = Company.CurrentResourceID, json = relJson }, transaction: trans);
		}
		private void CopyTags(ApiExecution execution, List<ProcessDiagramCopyMapper> copyMappers, SqlConnection conn, SqlTransaction trans)
		{
			var assetMap = JsonConvert.SerializeObject(copyMappers);
			conn.Execute($@"
				drop table if exists #assetsMap
				create table #assetsMap(
					oldUid uniqueidentifier,
					keyUid uniqueidentifier
				)

				 insert into #assetsMap
				select * from OPENJSON(@assetMap)
				with (	oldUid uniqueidentifier '$.oldUid',
						keyUid uniqueidentifier '$.keyUid')

				insert into AssetTag (uid, AssetID,TagId,CreatedOn, CreatedBy)
				select newid(),NewAsset.Id, ATAG.TagId, getutcdate(), @resourceId 
				from #assetsMap
					inner join api.ExecutionDiagramAsset eda on eda.executionitemuid = #assetsMap.keyuid
					inner join Asset A on a.uid = #assetsMap.oldUid
					inner join Asset NewAsset on NewAsset.uid = eda.uid
					inner join AssetTag ATAG on ATAG.AssetId = A.Id
					where eda.executionid = @executionid and eda.Action <> 'Delete'
							", new { execution.ExecutionID, resourceId = Company.CurrentResourceID, assetMap }, transaction: trans);
		}

		public IEnumerable<ProcessDiagramBadge> GetDiagramAssetBadges(Guid assetUid)
		{
			var badgesSql = $@"
				declare @diagram nvarchar(max) = (
				select apd.Diagram  as json from asset a 
					inner join AssetProcessDiagram apd on apd.AssetID = a.ID
				where a.uid = @assetUid)

				;with links as (
				SELECT  
					JSON_VALUE(nda.value, '$.key') AS [AssetUid]
				FROM OPENJSON(@diagram, '$.nodeDataArray') as nda)
				select a.uid as AssetUid, sum(rels.cnt) as RelationshipCount from links
				inner join Asset A on a.uid = links.AssetUid
				cross apply(
				select count(*) from [Intersect] I where A.Object = I.Object and A.ObjectID = I.ObjectID
				union 
				select count(*) from [Intersect] I where A.ObjectID = I.SubjectID AND a.Object = i.Subject
				)Rels(cnt)
			group by a.uid";

			var response = Company.Query<ProcessDiagramBadge>(badgesSql, new { assetUid }, ApiTimeout);
			return response;
		}

		public async Task<byte[]> GetDiagramExcel(Asset asset, byte[] image)
		{
			var document = new SLDocument();
			document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Process");

			var assetType = Company.AssetTypes.FirstOrDefault(x => x.ID == asset.AssetTypeID);

			await AssetRepository.PopulateSheetForAssetTypeAndAssets(document, assetType, new List<Guid>() { asset.uid });
			await GetDiagramWorkflowSheet(asset, document);
			await GetSheetsForDiagramTypes(asset, document);
			await GetSheetsForRelatedAssets(asset, document);

			document.AddWorksheet("Diagram");
			document.SelectWorksheet("Diagram");

			var picture = new SLPicture(image, DocumentFormat.OpenXml.Packaging.ImagePartType.Png);
			document.InsertPicture(picture);
			var stream = new MemoryStream();
			document.SaveAs(stream);
			byte[] bytes = stream.ToArray();
			return bytes;
		}

		private class DiagramAssetRelationshipModel
		{
			[JsonProperty("DiagramAssetUid")]
			public Guid DiagramAssetUid { get; set; }

			[JsonProperty("AssetUid")]
			public Guid AssetUid { get; set; }

			[JsonProperty("AssetTypeUid")]
			public Guid AssetTypeUid { get; set; }

			[JsonProperty("AssetTypeName")]
			public string AssetTypeName { get; set; }

			[JsonProperty("StepNo")]
			public string StepNo { get; set; }

			[JsonProperty("DiagramAssetName")]
			public string DiagramAssetName { get; set; }


			[JsonProperty("DiagramAssetId")]
			public int DiagramAssetId { get; set; }

			[JsonProperty("PredicateUid")]
			public Guid PredicateUid { get; set; }
		}

		private async Task GetSheetsForRelatedAssets(Asset asset, SLDocument document)
		{
			var relModels = await Company.QueryAsync<DiagramAssetRelationshipModel>(@"declare @diagram nvarchar(max) = (
				select apd.Diagram  as json from asset a 
					inner join AssetProcessDiagram apd on apd.AssetID = a.ID
				where a.uid = @assetUid)

				;with links as (
				SELECT  
					JSON_VALUE(nda.value, '$.key') AS [AssetUid]
				FROM OPENJSON(@diagram, '$.nodeDataArray') as nda)
				select a.id as DiagramAssetId, a.uid as DiagramAssetUid, FD.FormattedValue AS 'StepNo', FD2.FormattedValue AS 'DiagramAssetName', o.uid as AssetUid,AT.uid AS AssetTypeUid, AT.Name as AssetTypeName, p.UID as PredicateUid  from links
				inner join Asset A on a.uid = links.AssetUid
				left join FieldDetail FD ON FD.AssetId = A.Id and FD.Name = 'StepNo'
				left join FieldDetail FD2 ON FD2.AssetId = A.Id and FD2.Name = 'Name'
				inner join [Intersect] I on I.Subject = A.Object and I.SubjectID = A.ObjectID
				inner join [IntersectType] IT on IT.ID = I.IntersectTypeID
				inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 16
				inner join Asset O on O.Object = I.Object and O.ObjectID = I.objectid
				inner join AssetType AT on AT.Id = O.AssetTypeID
				order by FD.FormattedValue,FD2.FormattedValue
				", new { assetUid = asset.uid }, ApiTimeout);

			foreach (var assetTypeGroup in relModels.GroupBy(x => x.AssetTypeUid))
			{
				var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == assetTypeGroup.Key);
				string relatedSheetName = ("Related " + assetType.Name).GetSafeSheetName();

				document.AddWorksheet(relatedSheetName);
				document.SelectWorksheet(relatedSheetName);

				var par = new List<KeyValuePair<string, string>>();
				var assetUidForParam = assetTypeGroup.Select(x => x.AssetUid).Distinct().Select(x => x.ToString());
				par.Add(new KeyValuePair<string, string>("_assetUid", string.Join(",", assetUidForParam)));
				par.Add(new KeyValuePair<string, string>("includeParent", "true"));
				par.Add(new KeyValuePair<string, string>("_pagesize", assetUidForParam.Count().ToString()));
				var assets = await AssetRepository.GetAssets(assetType, par, true);

				var hierarchy = Company.IntersectTypes
				.FirstOrDefault(x => x.Object == assetType.Object && x.ObjectID == assetType.ObjectID && x.Predicate.Type == PredicateType.InterTypeHierarchy);

				bool includeParent = true;

				if (hierarchy == null)
				{
					includeParent = false;
				}
				
				var typesToAvoid = new List<string>() {
					DataType.ComplexRelationLookup.ToString(),
					DataType.DataTableSelect.ToString(),
					DataType.OwnershipLookup.ToString()
					};

				List<FieldType> fields = new List<FieldType>();

				var guid = Guid.NewGuid().ToString().Replace("-", "");
				fields.Add(new FieldType { Type = "number", Name = guid + "StepNo", FriendlyName = "Step No" });
				fields.Add(new FieldType { Type = "string", Name = guid + "DiagramAssetName", FriendlyName = "Diagram Asset Name" });

				if (includeParent)
				{
					fields.Add(new FieldType { Type = "string", Name = "ParentDisplayName", FriendlyName = "Parent" });
				}

				fields.AddRange(Company.FieldTypes.Where(f => f.AssetTypeID == assetType.ID).OrderBy(x => x.ColumnOrder).ThenBy(x => x.FriendlyName).ToList());


				fields.Add(new FieldType { Type = "string", Name = guid + "UID", FriendlyName = "Diagram Asset UID" });
				fields.Add(new FieldType { Type = "number", Name = guid + "ID", FriendlyName = "Diagram Asset ID" });
				fields.Add(new FieldType { Type = "string", Name = guid + "URL", FriendlyName = "Diagram URL" });

				fields.Add(new FieldType { Type = "string", Name = "AssetUid", FriendlyName = "Asset UID" });
				fields.Add(new FieldType { Type = "number", Name = "AssetId", FriendlyName = "Asset ID" });
				int index = 1;

				foreach (var field in fields)
				{
					if (typesToAvoid.Contains(field.Type))
					{
						continue;
					}
					document.SetCellValue(1, index++, field.FriendlyName);
				}

				document.SetCellValue(1, index++, "Url");
				var rowData = assets.items.ToList();

				var data = new List<IDictionary<string, object>>();
				foreach (var diaAsset in assetTypeGroup)
				{
					var exportItem = new Dictionary<string, object>();
					var relatedAsset = rowData.FirstOrDefault(x => x.AssetUid == diaAsset.AssetUid);
					exportItem.Add(guid + "StepNo", diaAsset.StepNo);
					exportItem.Add(guid + "DiagramAssetName", diaAsset.DiagramAssetName);
					exportItem.Add(guid + "UID", diaAsset.DiagramAssetUid);
					exportItem.Add(guid + "ID", diaAsset.DiagramAssetId);
					exportItem.Add(guid + "URL", $"asset/{asset.uid}/Process");

					if (relatedAsset != null)
					{
						var values = (relatedAsset as IDictionary<string, object>);
						foreach (var item in values)
						{
							exportItem.Add(item.Key, item.Value);
						}
					}
					data.Add(exportItem);
				}


				int rowNumber = 1;

				foreach (var row in data)
				{
					index = 1;
					rowNumber++;
					var rowValues = row;

					foreach (var field in fields)
					{
						if (typesToAvoid.Contains(field.Type))
						{
							continue;
						}

						if (rowValues.ContainsKey(field.Name))
						{

							if (field.Name == "Color")
							{
								string val = extractColorNameFromJSON((string)rowValues[field.Name]);
								setCellValueFromField(document, rowNumber, index, field, val);
							}
							else
							{
								var val = rowValues[field.Name];
								setCellValueFromField(document, rowNumber, index, field, val);
							}

						}

						index++;
					}

					if (rowValues.ContainsKey("AssetUid"))
					{
						document.SetCellValue(rowNumber, index, $"asset/{rowValues["AssetUid"]}");
					}
				}

				SetExcelColumnWidths(document, fields);
			}
		}

		private async Task GetSheetsForDiagramTypes(Asset asset, SLDocument document)
		{
			var types = await Company.QueryAsync<dynamic>(@"declare @diagram nvarchar(max) = (
				select apd.Diagram  as json from asset a 
					inner join AssetProcessDiagram apd on apd.AssetID = a.ID
				where a.uid = @assetUid)

				;with links as (
				SELECT  
					JSON_VALUE(nda.value, '$.key') AS [AssetUid]
				FROM OPENJSON(@diagram, '$.nodeDataArray') as nda)
				select at.uid as AssetTypeUid, at.Name as AssetTypeName, string_agg(cast(a.uid as nvarchar(max)),',') as assets from links
				inner join Asset A on a.uid = links.AssetUid
				inner join AssetType at on at.id = a.AssetTypeID
				group by at.uid, at.name", new { assetUid = asset.uid }, ApiTimeout);

			foreach (var type in types)
			{
				var rowValues = (type as IDictionary<string, object>);

				var name = rowValues["AssetTypeName"];
				var assetTypeUid = Guid.Parse(rowValues["AssetTypeUid"].ToString());
				var assets = rowValues["assets"];

				string detailSheetName = (name + " Details").GetSafeSheetName();
				document.AddWorksheet(detailSheetName);
				document.SelectWorksheet(detailSheetName);
				var at = Company.AssetTypes.FirstOrDefault(x => x.uid == assetTypeUid);
				await AssetRepository.PopulateSheetForAssetTypeAndAssets(document, at, assets.ToString().Split(',').Select(x => Guid.Parse(x)).ToList());
			}
		}

		private async Task GetDiagramWorkflowSheet(Asset asset, SLDocument document)
		{
			string detailSheetName = "Asset Item Details";
			document.AddWorksheet(detailSheetName);
			document.SelectWorksheet(detailSheetName);

			var diagramSql = $@"
								declare @diagram nvarchar(max) = (
								select apd.Diagram  as json from asset a 
									inner join AssetProcessDiagram apd on apd.AssetID = a.ID
								where a.uid = @assetUid)

								drop table if exists #nodes
								create table #nodes(
									AssetUid uniqueidentifier
								)

								drop table if exists #links
								create table #links(
									FromUid uniqueidentifier,
									ToUid uniqueidentifier,
									LabelUid uniqueidentifier
								)

								insert into #nodes
								SELECT  
									JSON_VALUE(nda.value, '$.key') AS [FromUid]
								FROM OPENJSON(@diagram, '$.nodeDataArray') nda

								insert into #links
								SELECT  
									JSON_VALUE(nda.value, '$.from') AS [FromUid],
									JSON_VALUE(nda.value, '$.to') AS [ToUid],
									JSON_VALUE(nda.value, '$.labelUid') as [LabelUid]
								FROM OPENJSON(@diagram, '$.linkDataArray') as nda

								;with cte_links as (select 
								n.AssetUid as FromUid,
								l.ToUid,
								l.LabelUid
								from #nodes n
								left join #links l on l.FromUid = n.AssetUid 	
								)
								select 
								try_cast(f1_step.FormattedValue as decimal(15,3)) as 'Step No',
								f1_name.FormattedValue as 'Name',
								f1_gov.FormattedValue as 'Governance Role',
								case at1.FlowObjectType
														when 1 then 'Event'
														when 2 then 'Activity'
														when 3 then 'Gateway'
													end as 'Flow Object Type',
								at1.Name as 'Diagram Asset Type',
								CL.Value as 'Next Asset Connector Label',
								try_cast(f2_step.FormattedValue as decimal(15,3)) as 'Next Asset Step No',
								f2_name.FormattedValue as 'Next Asset Name',
								lower(a1.uid) as 'Asset UID',
								a1.id as 'Asset ID',
								'asset/'+ cast(lower(a1.uid) as nvarchar(36)) as 'Asset URL',
								lower(a2.uid) as 'Next Asset UID',
								a2.id as 'Next Asset ID',
								 'asset/'+ cast(lower(a2.uid) as nvarchar(36)) as 'Next Asset URL'
								from cte_links l
								left join Asset a1 on a1.uid = l.fromuid
								left join AssetType at1 on at1.ID = a1.AssetTypeID
								left join FieldDetail f1_name on f1_name.Name = 'Name' and f1_name.AssetId = a1.id 
								left join FieldDetail f1_step on f1_step.Name = 'StepNo' and f1_step.AssetId = a1.id 
								left join FieldDetail f1_gov on f1_gov.Name = 'GovernanceRole' and f1_gov.AssetId = a1.id 
								left join Asset a2 on a2.uid = l.ToUid
								left join FieldDetail f2_name on f2_name.Name = 'Name' and f2_name.AssetId = a2.id 
								left join FieldDetail f2_step on f2_step.Name = 'StepNo' and f2_step.AssetId = a2.id 
								left join ConnectorLabel CL on CL.uid = l.labeluid
								order by try_cast (f1_step.FormattedValue as decimal(15,3)) asc, f1_name.FormattedValue
								";

			List<string> diagramFields = new List<string>() {
			"Step No","Name", "Governance Role","Flow Object Type",
			"Diagram Asset Type", "Next Asset Connector Label","Next Asset Step No",
			"Next Asset Name","Asset UID","Asset ID","Asset URL",
			"Next Asset UID","Next Asset ID","Next Asset URL"
			};
			var diagram = await Company.QueryAsync<dynamic>(diagramSql, new { assetUid = asset.uid }, ApiTimeout);
			int index = 1;

			foreach (var field in diagramFields)
			{
				document.SetCellValue(1, index++, field);
			}

			int rowNumber = 1;
			foreach (var row in diagram)
			{
				index = 1;
				rowNumber++;
				var rowValues = (row as IDictionary<string, object>);

				foreach (var field in diagramFields)
				{
					if (rowValues.ContainsKey(field))
					{
						var fieldType = new FieldType();
						var val = rowValues[field];
						if (field == "Step No" || field == "Next Asset Step No" || field == "Asset ID" || field == "Next Asset ID")
						{
							fieldType.Type = "Decimal";
						}
						setCellValueFromField(document, rowNumber, index, fieldType, val);
					}

					index++;
				}
			}

			document.AutoFitColumn(0, diagramFields.Count - 1);
		}

		private string extractColorNameFromJSON(string jsonString)
		{
			if (!string.IsNullOrEmpty(jsonString))
			{
				var colorObj = JObject.Parse(jsonString);
				return (string)colorObj["Name"] ?? "";
			}
			return "";
		}
	}
}
