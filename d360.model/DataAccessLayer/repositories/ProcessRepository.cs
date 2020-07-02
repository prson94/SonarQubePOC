using d360.core;
using d360.core.entities;
using d360.core.entities.Process;
using d360.core.enums;
using d360.model.DataAccessLayer.repositories;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class ProcessRepository : BaseRepository, IProcessRepository
    {
        internal ICompanyContext Company;

        public ProcessRepository(ICompanyContext context) : base(context)
        {
            this.Company = context;
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

            var nodes = await Company.QueryAsync<dynamic>(sql, new { assetUid, predicateType = (int)PredicateType.Diagram });
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
	                    select ft.Name, f.Value from Field f 
	                    inner join FieldType ft on f.FieldTypeID = ft.ID
	                    where assetid = a.id for json path
	                    )field(json)
                    where A.uid in @assetUids", new { assetUids }).ToList();

            foreach (var item in nodesExpandedData)
            {
                var json = JsonConvert.SerializeObject(item);
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                var node = model.nodeDataArray.FirstOrDefault(x => x.AssetUid == Guid.Parse(dictionary["uid"]));
                node["icon"] = dictionary["icon"];
                node["category"] = dictionary["category"];
                node["refItemColor"] = dictionary["refItemColor"];
                node["assetTypeUid"] = dictionary["assetTypeUid"];
                node["assetTypeName"] = dictionary["assetTypeName"];
                node["objectId"] = dictionary["objectId"];

                if (dictionary["fields"] != null)
                {
                    var arr = JsonConvert.DeserializeObject<JArray>(dictionary["fields"]);
                    foreach (JObject field in arr)
                    {
                        node[field["Name"].ToString()] = field["Value"].ToString();
                    }
                }
            }

            return model;
        }

        public List<ValidationError> UpdateProcessDiagram(ApiExecution execution, ProcessDiagramModel model, List<NodeData> toAdd, List<NodeData> toUpdate, List<NodeData> toDelete, long targetAssetId)
        {
            var validationRes = new List<ValidationError>();

            if (Company.Database.Connection.State != ConnectionState.Open)
                Company.Connection.Open();

            //Validation passed lets do some work
            var totalCount = toAdd.Count + toDelete.Count + toUpdate.Count;
            execution.Method = "Process";
            execution.ProcessingStartedOn = DateTime.UtcNow;
            Company.Add(execution);

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

delete Asset where Uid in (select S.Uid from api.executiondiagramasset S where S.Action = 'Delete' and s.executionid = @ExecutionID)





", new { execution.ExecutionID }, transaction: trans);


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
		where eda.executionid = @executionid and edaf.executionid = @executionid
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
values		(S.ID, S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, getutcdate());

", new { executionId = execution.ExecutionID, resourceId = Company.CurrentResourceID }, transaction: trans);



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
                    var simpleModel = new ProcessDiagramModel();
                    simpleModel.@class = "ProcessDiagram";
                    simpleModel.linkDataArray = model.linkDataArray;

                    simpleModel.nodeDataArray = new List<NodeData>();
                    foreach (var node in model.nodeDataArray)
                    {
                        var simpleNode = new NodeData();
                        simpleNode.Add("key", node.AssetUid.ToString());
                        simpleNode.Add("loc", node["loc"]);
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
                    trans.Rollback();

                    execution.ErrorMessage = ex.GetFullExceptionData(false);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                    validationRes.Add(new ValidationError() { Error = ex.Message });


                }
            }


            return validationRes;
        }

    }
}
