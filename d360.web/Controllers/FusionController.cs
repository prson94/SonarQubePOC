using d360.core;
using d360.core.entities;
using d360.core.helpers;
using d360.extensions;
using d360.model;
using d360.web.Models.Attributes;
using Newtonsoft.Json;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/fusion"), Authorize]
    public class FusionController : BaseController
    {
        #region DI

        IQueueSource Queue;
        IStorageProvider Storage;

        public FusionController(CommunityContext community, CompanyContext company, IStorageProvider storage, IQueueSource queue)
            : base(community, company)
        {
            Queue = queue;
            Storage = storage;
        }

        #endregion

        #region Partials

        [Route("_FusionExecutionRawLog"), NonNullableParameters]
        public ContentResult _FusionExecutionRawLog(int id)
        {
            var execution = Company.Query<dynamic>(@"select * from fusion.Execution where ID = @id", new { id }).SingleOrDefault();
            
            return Content(Storage.GetFileContentsAsString($"bulk-fusion-{Company.CurrentCompanyID}", execution.RawLogFileName), "application/json");
        }

        [Route("{typeID:int}/configurations/{id:int}/template/{attributeTypeID:int}"), FileDownload, HttpGet]
        public FileResult GetFusionManualLoadTemplateForAttributeType(int typeID, int id, int attributeTypeID)
        {
            var fusion = Company.GetById<Fusion>(id, i => i.FusionType.FusionAttributeTypes);

            var document = new SLDocument();
            var requiredStyle = new SLStyle { Font = new SLFont { Bold = true } };

            var targetAttributeType = fusion.FusionType.FusionAttributeTypes.SingleOrDefault(i => i.ID == attributeTypeID);
            var targetAttributeTypeFields = Company.Filter<FieldType>(i => i.Object == "FusionAttributeType" && i.ObjectID == attributeTypeID).ToList();
            
            if (targetAttributeType != null)
            {
                var pushValue = 1;
                document.AddWorksheet(string.Format("{0}", targetAttributeType.Name));

                var parentAttributeTypeIDs = new List<int>();
                int? parentID = targetAttributeType.ParentID;

                #region Determine the correct order of the fusion attribute IDs (1. Schema / 2. Table / 3. Column)

                while (parentID.HasValue)
                {
                    var parent = fusion.FusionType.FusionAttributeTypes.SingleOrDefault(i => i.ID == parentID.Value);
                    if (parent != null)
                    {
                        parentAttributeTypeIDs.Insert(0, parent.ID);
                        parentID = parent.ParentID;
                    }
                }

                #endregion

                // List out the parents, in correct order.
                foreach (var nodeID in parentAttributeTypeIDs)
                {
                    var t = fusion.FusionType.FusionAttributeTypes.Single(i => i.ID == nodeID);
                    document.SetCellValue(1, pushValue, t.Name);
                    document.SetCellStyle(1, pushValue, requiredStyle);
                    pushValue++;
                }

                document.SetCellValue(1, pushValue, targetAttributeType.Name);
                document.SetCellStyle(1, pushValue, requiredStyle);
                pushValue++;

                for (int i = 0; i < targetAttributeTypeFields.Count; i++)
                {
                    document.SetCellValue(1, i + pushValue, targetAttributeTypeFields[i].Name);
                    if (targetAttributeTypeFields[i].IsRequired)
                    {
                        document.SetCellStyle(1, i + pushValue, requiredStyle);
                    }
                }
            }

            document.DeleteWorksheet("Sheet1");

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", string.Format("Load Template for {0}.xlsx", fusion.FusionType.Name));
        }

        #endregion

        #region Json

        string parentSql = @"
with h as	(
			select	ID,
					ParentID,
                    Name,
					0 as [Level]
			from	FusionAttributeType
			where	ID = @t
			union all
			select	P.ID,
					P.ParentID,
                    P.Name,
					C.[Level] + 1 as [Level]
			from	FusionAttributeType P
					inner join h as C on C.ParentID = P.ID
			)

select * from h where ID <> @t order by h.[Level] desc;
";


        internal class SqlFieldModel
        {
            public string FieldColumnName { get; set; }
            public string FieldName { get; set; }
            public string FieldFriendlyName { get; set; }
            public string FieldJoin { get; set; }
            public int JoinOrder { get; set; }
        }

        [Route("ExportItemsByAttributeType"), FileDownload, NonNullableParameters]
        public FileResult ExportItemsByAttributeType(int fusionID, int fusionAttributeTypeID, string sortDataField, string sortOrder)
        {
            var type = "FusionAttributeType";
            
            var fusionAttributeTypeName = Company.FusionAttributeTypes.Where(x => x.ID == fusionAttributeTypeID).Single().Name;

            var pluralFusionAttributeName = "Defalut";
            if(PluralCultureHelper.IsNeutralCultureEnglish())
            {
                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                pluralFusionAttributeName = pluralize.Pluralize(fusionAttributeTypeName);
            }
            
            var sqlFieldModels = new List<SqlFieldModel>();

            #region Parents

            var parents = Company.Query<dynamic>(parentSql, new { t = fusionAttributeTypeID }).ToList();

            //Parent columns have be listed in ASC order by Level.
            parents.ForEach(i =>
            {
                var thisJoin = ((int)i.Level == 1) ?
                    $" inner join FusionAttribute P{i.Level} on P{i.Level}.ID = A.ParentID and P{i.Level}.Deleted = 0" :
                    $" inner join FusionAttribute P{i.Level} on P{i.Level}.ID = P{i.Level - 1}.ParentID";
                

                sqlFieldModels.Add(new SqlFieldModel
                {
                    FieldColumnName = $"Parent{i.ID}",
                    FieldName = $"P{i.Level}.Name as Parent{i.ID}",
                    FieldFriendlyName = i.Name,
                    FieldJoin = thisJoin,
                    JoinOrder = (int)i.Level,

                });
            });
            
            #endregion

            sqlFieldModels.Add(new SqlFieldModel { FieldColumnName = "ID", FieldName = "A.ID", FieldFriendlyName = "ID" });
            sqlFieldModels.Add(new SqlFieldModel { FieldColumnName = "AssetID", FieldName = "B.ID as AssetID", FieldFriendlyName = "Asset ID" });
            sqlFieldModels.Add(new SqlFieldModel { FieldColumnName = "Name", FieldName = "A.Name", FieldFriendlyName = fusionAttributeTypeName });

            #region Dynamic Fields

            var fields = Company.Filter<FieldType>(i => i.Object == type && i.ObjectID == fusionAttributeTypeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();

            fields.ForEach(f => {
                var name = $"Field{f.ID}";
                
                sqlFieldModels.Add(new SqlFieldModel
                {
                    FieldColumnName = $"{name}",
                    FieldName = $"{name}_T.FormattedValue as [{name}]",
                    FieldFriendlyName = f.FriendlyName,
                    FieldJoin = $"inner join FieldType { name }_TT on { name }_TT.ID = { f.ID } and { name }_TT.Object = 'FusionAttributeType' and { name }_TT.ObjectID = @t left join Field { name }_T on { name }_T.ObjectType = 'FusionAttribute' and { name }_T.ObjectID = A.ID and { name }_T.FieldTypeID = { name }_TT.ID ",
                    JoinOrder = 100
                });
            });

            #endregion
                       
            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("f", fusionID);
            dbArgs.Add("t", fusionAttributeTypeID);

            #region Query

            string columns = string.Join(", ", sqlFieldModels.Where(c => !c.FieldName.Contains("P.IntersectType")).Select(c => c.FieldName));
            string joins = string.Join(" ", sqlFieldModels.Where(o => !string.IsNullOrEmpty(o.FieldJoin)).OrderBy(o => o.JoinOrder).Select(o => o.FieldJoin));
            
            var sql = $@"
select  {columns}
from	FusionAttribute A
        inner join Asset B on B.[Object] = 'FusionAttribute' and B.ObjectID = A.ID 
        {joins}       
       
where   A.FusionID = @f 
        and A.FusionAttributeTypeID = @t 
        and A.Deleted = 0";

            sql = $@"select * from ({sql}) A";
            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder);

            #endregion

            var query = Company.Query<dynamic>(sql, dbArgs).ToList();

            #region Create the list sheet

            var document = new SLDocument();
            var defaultSheet = pluralFusionAttributeName;
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, defaultSheet);
            document.SelectWorksheet(defaultSheet);

            var col = 1;
            var row = 1;
            var totalColumns = sqlFieldModels.Count;

            #region Header
            foreach (var prop in sqlFieldModels)
            {
                document.SetCellValue(row, col, prop.FieldFriendlyName);
                col++;
            }
            
            #endregion

            document.AutoFitColumn(1, totalColumns);

            foreach (var item in query)
            {
                var obj = (item as IDictionary<string, object>);
                col = 1;
                row++;
                foreach (var prop in sqlFieldModels)
                {
                    document.SetCellValue(row, col, (obj[prop.FieldColumnName] != null) ? obj[prop.FieldColumnName].ToString() : "");
                    col++;
                }
            }

            

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"{pluralFusionAttributeName}.xlsx");
        }

        [Route("ItemsByAttributeType"), NonNullableParameters]
        public JsonNetResult ItemsByAttributeType(int fusionID, int fusionAttributeTypeID, string sortDataField, string sortOrder, int pagenum, int pagesize)
        {
            var joins = "";
            var columns = "";

            var filterjoins = "";
            var filtercolumns = "";

            var queryParameters = Request.Params;
            var filterscount = 0;
            var filterFields = new List<string>();
            if (int.TryParse(queryParameters["filterscount"], out filterscount))
            {
                for (int i = 0; i < filterscount; i++)
                {
                    var fField = queryParameters["filterdatafield" + i];
                    if (!string.IsNullOrEmpty(fField))
                    {
                        filterFields.Add(fField);
                    }
                }
            }

            // Check to see if any field types for this fusion attribute type is set to editable, and that current user has permissions to edit anything in the first place.
            var editable = Company.Any<FieldType>(i => i.Object == "FusionAttributeType" && i.ObjectID == fusionAttributeTypeID && i.IsEditable) ? 1 : 0;
            var hasEditRights = Company.HasAssetPermission(SystemObjects.Fusion, fusionID, core.enums.Permission.ModifyAsset);
            if (!hasEditRights) editable = 0;

            getDynamicFieldJoinStatements(fusionAttributeTypeID, "FusionAttribute", filterFields, out joins, out filterjoins, out columns, out filtercolumns, false);

            #region Parents

            var parents = Company.Query<dynamic>(parentSql, new { t = fusionAttributeTypeID }).ToList();

            //Parent columns have be listed in DESC order by Level.
            var parentFilterColumnText = "";
            var parentQueryColumnText = "";

            var parentFilterPresent = parents.Count > 1 && filterFields.Any(i => i.StartsWith("Parent"));
            parents.ForEach(i =>
            {
                var thisColumn = $", P{i.Level}.Name as Parent{i.ID}";
                parentQueryColumnText += thisColumn;
                if (parentFilterPresent)
                {
                    parentFilterColumnText += thisColumn;
                }
                else
                {
                    if (filterFields.Contains($"Parent{i.ID}"))
                    {
                        parentFilterColumnText += thisColumn;
                    }
                }
            });

            var sField = sortDataField.StartsWith("Field") ? sortDataField.ReplaceFirst("Field", "") : "";
            int sFieldTypeId=0;
            string sortFieldType = null;  
            if (!string.IsNullOrEmpty(sField) && Int32.TryParse(sField, out sFieldTypeId))
            {
                sortFieldType = Company.Filter<FieldType>(x => x.ID == sFieldTypeId).SingleOrDefault().Type;
            }
            //Parent joins have be listed in ASC order by Level.
            var parentQueryJoinText = "";
            parents.OrderBy(i => i.Level).ToList().ForEach(i =>
            {
                var thisJoin = ((int)i.Level == 1) ?
                    $" inner join FusionAttribute P{i.Level} on P{i.Level}.ID = A.ParentID and P{i.Level}.Deleted = 0" :
                    $" inner join FusionAttribute P{i.Level} on P{i.Level}.ID = P{i.Level - 1}.ParentID";

                parentQueryJoinText += thisJoin;
            });
                        
            #endregion

            if (columns.Contains("[type]"))
                columns = columns.Replace("[type]", "[_type]");

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("f", fusionID);
            dbArgs.Add("t", fusionAttributeTypeID);

            string profileColumns = "";
            string profileJoins = "";
            if(queryParameters["target"]=="DataProfile")
            {
                profileColumns = @" ,ADP.ID as DataProfileID,
                                    ADP.[RowCount] as RowCounts,
                                    ADP.Uniqueness,
                                    ADP.UniqueCount,
                                    ADP.Completeness,
                                    ADP.NullCount,
                                    ADP.BlankCount,
                                    ADP.DataType,
                                    ADP.MinimumValue,
                                    ADP.MaximumValue,
                                    ADP.Precision,
                                    ADP.Scale,
                                    ADP.Average,
                                    ADP.Median,
                                    ADP.StandardDeviation,
                                    ADP.Top10Values,
                                    ADP.ProcessIdentifier";
                profileJoins = @"inner join Asset AA on AA.ObjectID=A.ID and AA.Object='FusionAttribute'
                                 inner join  AssetDataProfile ADP on AA.ID = ADP.AssetID";
            }
            #region Count SQL

            var countSql = $@"
select  A.ID, 
        A.Name, 
        A.FusionAttributeTypeID,
        'FusionAttribute' as [Type]
        {profileColumns}
        {filtercolumns} 
        {parentFilterColumnText} 
from	FusionAttribute A
{profileJoins}
{parentQueryJoinText} {filterjoins}
where   A.FusionID = @f 
        and A.FusionAttributeTypeID = @t 
        and A.Deleted = 0";

            countSql = $@"select count(1) from ({countSql}) A";
            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            countSql += " OPTION (RECOMPILE)";
            var total = Company.Query<int>(countSql, dbArgs).First();

            #endregion

            #region Query

            var sql = $@"
select  A.ID 
        , A.Name 
        , A.FusionAttributeTypeID
        , 'FusionAttribute' as [Type]
        , cast({editable} as bit) as IsEditable
       {profileColumns}
        {columns} 
        {parentQueryColumnText} 
from	FusionAttribute A 
{profileJoins}
{parentQueryJoinText} {joins} 
where A.FusionID = @f and A.FusionAttributeTypeID = @t and A.Deleted = 0";

            sql = $@"select * from ({sql}) A";
            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder, sortFieldType:sortFieldType);
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            sql += " OPTION (RECOMPILE)";

            var query = Company.Query<dynamic>(sql, dbArgs);

            #endregion

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Formatting.None };
        }

        [Route("ExportQueryItemsByAttributeType"), FileDownload, NonNullableParameters]
        public FileResult ExportQueryItemsByAttributeType(int fusionID, int fusionQueryAttributeTypeID, string sortDataField, string sortOrder)
        {
            var type = "FusionQueryAttributeType";
            var fusionQueryAttributeTypeName = Company.FusionQueryAttributeTypes.Where(x => x.ID == fusionQueryAttributeTypeID).Single().Name;

            var pluralFusionQueryAttributeTypeName = "Defalut";
            if (PluralCultureHelper.IsNeutralCultureEnglish())
            {
                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                pluralFusionQueryAttributeTypeName = pluralize.Pluralize(fusionQueryAttributeTypeName);
            }

            var sqlFieldModels = new List<SqlFieldModel>();

            sqlFieldModels.Add(new SqlFieldModel { FieldColumnName = "ID", FieldName = "A.ID", FieldFriendlyName = "ID" });

            #region Dynamic Fields

            var fields = Company.Filter<FieldType>(i => i.Object == type && i.ObjectID == fusionQueryAttributeTypeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();

            fields.ForEach(f => {
                var name = $"Field{f.ID}";
                
                sqlFieldModels.Add(new SqlFieldModel
                {
                    FieldColumnName = $"{name}",
                    FieldName = $"{name}_T.FormattedValue as [{name}]",
                    FieldFriendlyName = f.FriendlyName,                    
                    FieldJoin = $@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{type}' and {name}_TT.ObjectID = A.FusionQueryAttributeTypeID and {name}_TT.IsListable = 1 
                                    left join Field {name}_T on {name}_T.ObjectType = 'FusionQueryAttribute' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {name}_TT.ID ",
                    JoinOrder = 100
                });
            });

            #endregion

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("t", fusionQueryAttributeTypeID);

            #region Query

            string columns = string.Join(", ", sqlFieldModels.Select(c => c.FieldName)); 
            string joins = string.Join(" ", sqlFieldModels.Where(o => !string.IsNullOrEmpty(o.FieldJoin)).OrderBy(o => o.JoinOrder).Select(o => o.FieldJoin));

            var sql = $@"
select  {columns} 
from	FusionQueryAttribute A 
        {joins}
where   A.FusionQueryAttributeTypeID = @t 
        and A.Deleted = 0";

            sql = $@"select * from ({sql}) A";
            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder, "ID");

            #endregion

            var query = Company.Query<dynamic>(sql, dbArgs).ToList();

            #region Create the list sheet

            var document = new SLDocument();
            var defaultSheet = pluralFusionQueryAttributeTypeName;
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, defaultSheet);
            document.SelectWorksheet(defaultSheet);

            var col = 1;
            var row = 1;
            
            #region Header
            foreach (var prop in sqlFieldModels)
            {
                document.SetCellValue(row, col, prop.FieldFriendlyName);
                col++;
            }
            
            #endregion

            foreach (var item in query)
            {
                var obj = (item as IDictionary<string, object>);
                col = 1;
                row++;
                foreach (var prop in sqlFieldModels)
                {
                    document.SetCellValue(row, col, (obj[prop.FieldColumnName] != null) ? obj[prop.FieldColumnName].ToString() : "");
                    col++;
                }
            }
            
            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"{pluralFusionQueryAttributeTypeName}.xlsx");
        }

        [Route("QueryItemsByAttributeType"), NonNullableParameters]
        public JsonNetResult QueryItemsByAttributeType(int fusionID, int fusionQueryAttributeTypeID, string sortDataField, string sortOrder, int pagenum, int pagesize)
        {
            var joins = "";
            var columns = "";

            var filterjoins = "";
            var filtercolumns = "";

            var queryParameters = Request.Params;
            var filterscount = 0;
            var filterFields = new List<string>();
            if (int.TryParse(queryParameters["filterscount"], out filterscount))
            {
                for (int i = 0; i < filterscount; i++)
                {
                    var fField = queryParameters["filterdatafield" + i];
                    if (!string.IsNullOrEmpty(fField))
                    {
                        filterFields.Add(fField);
                    }
                }
            }

            getDynamicFieldJoinStatements(fusionQueryAttributeTypeID, "FusionQueryAttribute", filterFields, out joins, out filterjoins, out columns, out filtercolumns, false);

            if (columns.Contains("[type]"))
                columns = columns.Replace("[type]", "[_type]");

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("t", fusionQueryAttributeTypeID);

            #region Count SQL

            var countSql = $@"
select  A.ID, 
        A.FusionQueryAttributeTypeID,
        'FusionQueryAttribute' as [Type]
        {filtercolumns}
from	FusionQueryAttribute A {filterjoins}
where   A.FusionQueryAttributeTypeID = @t 
        and A.Deleted = 0";

            countSql = $@"select count(1) from ({countSql}) A";
            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            var total = Company.Query<int>(countSql, dbArgs).First();

            #endregion

            #region Query

            var sql = $@"
select  A.ID 
        , A.FusionQueryAttributeTypeID
        , 'FusionQueryAttribute' as [Type]
        {columns} 
from	FusionQueryAttribute A {joins}
where   A.FusionQueryAttributeTypeID = @t 
        and (A.Deleted = 0 or A.Deleted is null)";

            sql = $@"select * from ({sql}) A";
            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder, "ID");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);

            #endregion

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Formatting.None };
        }

        [Route("details/{type}/{id:int}")]
        public JsonResult FusionItemDetails(SystemObjects type, int id)
        {
            Dictionary<string, object> model = new Dictionary<string, object>();
            switch (type)
            {
                case SystemObjects.FusionAttribute:
                    var fusionAttribute = Company.GetById<FusionAttribute>(id);
                    if (fusionAttribute != null)
                    {
                        model["Name"] = fusionAttribute.Name;
                        model["TextPath"] = fusionAttribute.TextPath;
                        model["FusionID"] = fusionAttribute.FusionID;
                        model["FusionAttributeTypeID"] = fusionAttribute.FusionAttributeTypeID;

                        var asset = Company.Assets.Where(x => x.Object == "FusionAttribute" && x.ObjectID == id).FirstOrDefault();
                        if (asset != null)
                        {
                            model["AssetID"] = asset.ID;
                        }
                    }
                    break;
                case SystemObjects.FusionQueryAttribute:
                    var fusionQueryAttribute = Company.GetById<FusionQueryAttribute>(id, i => i.FusionQueryAttributeType);
                    if (fusionQueryAttribute != null)
                    {                     
                        model["FusionID"] = fusionQueryAttribute.FusionQueryAttributeType.FusionID;
                        model["FusionAttributeTypeID"]= fusionQueryAttribute.FusionQueryAttributeTypeID;
                    }
                    break;
            }
                    
            var sql = @"
            select 
                F.FormattedValue as FormattedValue,
	            FT.FriendlyName as FriendlyName,  
                FT.Type as DataType
            from
            [dbo].field F
            inner join [dbo].fieldtype FT on (F.FieldTypeID = FT.ID)
            where
	                F.[objectType] = @objectType
		                and
	                F.[objectId] = @objectId;    
            ";

            var fields = Company.Query<dynamic>(sql, new { objectType = new Dapper.DbString { Value = type.ToString(), IsFixedLength = true, Length = 50, IsAnsi = true }, objectId = id });
            List<dynamic> res = new List<dynamic>();

            foreach (var item in fields)
            {
                if (string.IsNullOrEmpty(item.FormattedValue)) continue;

                res.Add(new
                {
                    Name = item.FriendlyName,
                    Value = item.FormattedValue,
                    DataType =item.DataType,
                    Type=0
                });
            }
                        
            model["Fields"] = res;            

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [Route("profile/{type}/{id:int}")]
        public JsonResult FusionitemProfile(SystemObjects type, int id)
        {
            var profiles = Company.Query<dynamic>(@"select P.* from Asset A
                    inner join AssetProfile P on P.AssetID = A.ID
                    where A.[Object] = @type and A.ObjectID = @id", new { type = type.ToString(), id });

            return Json(profiles, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get all available fusion configurations for a specific type.  These configurations provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>A list of available fusion configurations.</returns>
        [Route("{id:int}/configurations")]
        public JsonNetResult GetConfigurationsByType(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return new JsonNetResult { Formatting = Formatting.Indented, Data = new { message = "You do not have permissions to view configurations." } };

            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Fusion", out joins, out columns, false, false);

            var querySql = string.Format(@"select	A.ID,
		A.Name,
		A.Description,
        A.FusionTypeID,
		T.Name as FusionType,
		substring(
        (
            select	',' + AD.TypeName  AS [text()]
            from	FusionOwner [FO]
					inner join AssetDetail AD on FO.AssetID  = AD.ID
            ORDER BY AD.TypeName
            For XML PATH ('')
        ), 2, 1000) as Owners,
        {0}
		A.Enabled
from	Fusion A {1} 
left join FusionType T on T.ID = A.FusionTypeID
where A.FusionTypeID = @id", columns, joins);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            return new JsonNetResult {
                Formatting = Formatting.None,
                Data = Company.Query<dynamic>(sql, new { id = id })
            };
        }

        #endregion
    }
}
