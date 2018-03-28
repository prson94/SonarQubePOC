using d360.core;
using d360.core.entities;
using d360.core.exceptions;
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
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Linq;

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
            var execution = Company.Query<dynamic>(@"select * from fusion.Execution where ID = @id", new { id = id }).SingleOrDefault();
            
            return Content(Storage.GetFileContentsAsString(string.Format("bulk-fusion-{0}", Company.CurrentCompanyID), execution.RawLogFileName), "application/json");
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

        internal class GenericFusionAttribute
        {
            public string SourceID { get; set; }
            public string ParentSourceID { get; set; }
            public int FusionAttributeTypeID { get; set; }
            public string Name { get; set; }
        }

        [Route("{typeID:int}/configurations/{id:int}/template/{attributeTypeID:int}"), HttpPost]
        public async Task<HttpStatusCodeResult> UploadFusionManualLoad(int typeID, int id, int attributeTypeID)
        {
            try
            {
                for(var indx = 0; indx < Request.Files.Count; indx++)
                {
                    var file = Request.Files[indx];
                    var fileExt = Path.GetExtension(file.FileName);
                    var target = new MemoryStream();
                    file.InputStream.CopyTo(target);

                    var xls = new SLDocument(target);
                    var stats = xls.GetWorksheetStatistics();
                    var endRowIndex = stats.EndRowIndex;
                    var currentRowIndex = 0;
                    var currentRowNumber = 1;

                    var fusion = Company.GetById<Fusion>(id, i => i.FusionType.FusionAttributeTypes);

                    var mXml = new XElement("ms");

                    var targetAttributeType = fusion.FusionType.FusionAttributeTypes.SingleOrDefault(i => i.ID == attributeTypeID);
                    var targetAttributeTypeFields = Company.Filter<FieldType>(i => i.Object == "FusionAttributeType" && i.ObjectID == attributeTypeID).ToList();

                    if (targetAttributeType != null)
                    {
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

                        var currentColumnIndex = 1;

                        var models = new List<Dictionary<string, string>>();

                        #region Parse raw ancestor nodes.

                        foreach (var nodeID in parentAttributeTypeIDs)
                        {
                            currentRowIndex = 2;    // Reset the current row index.
                            var sourceIDs = new List<string>();

                            while (currentRowIndex <= endRowIndex)
                            {

                                #region Create SourceID

                                var sourceID = "";
                                for (int i = 1; i <= currentColumnIndex; i++)
                                {
                                    sourceID += ((sourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                                }

                                #endregion

                                //Check to see if we already added this.
                                if (!sourceIDs.Any(i => i == sourceID))
                                {
                                    sourceIDs.Add(sourceID);

                                    #region Create ParentSourceID

                                    var parentSourceID = "";
                                    for (int i = 1; i < currentColumnIndex; i++)
                                    {
                                        parentSourceID += ((parentSourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                                    }

                                    #endregion

                                    var jsonFields = new Dictionary<string, string>();

                                    jsonFields.Add("Name", xls.GetCellValueAsString(currentRowIndex, currentColumnIndex));
                                    jsonFields.Add("SourceID", sourceID);
                                    jsonFields.Add("FusionAttributeTypeID", nodeID.ToString());
                                    if (!string.IsNullOrEmpty(parentSourceID))
                                    {
                                        jsonFields.Add("ParentSourceID", parentSourceID);
                                    }

                                    models.Add(jsonFields);


                                    currentRowNumber++;    // This number must be unique.  A unique number per fusion attribute.                            
                                }

                                currentRowIndex++;
                            }

                            currentColumnIndex++;
                        }

                        #endregion

                        #region Get the target fusion attribute type rows

                        currentRowIndex = 2;    // Reset the current row index.
                        while (currentRowIndex <= endRowIndex)
                        {
                            #region Create SourceID

                            var sourceID = "";
                            for (int i = 1; i <= currentColumnIndex; i++)
                            {
                                sourceID += ((sourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                            }

                            #endregion

                            #region Create ParentSourceID

                            var parentSourceID = "";
                            for (int i = 1; i < currentColumnIndex; i++)
                            {
                                parentSourceID += ((parentSourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                            }

                            #endregion

                            var jsonFields = new Dictionary<string, string>();

                            jsonFields.Add("Name", xls.GetCellValueAsString(currentRowIndex, currentColumnIndex));
                            jsonFields.Add("SourceID", sourceID);
                            jsonFields.Add("FusionAttributeTypeID", attributeTypeID.ToString());
                            if (!string.IsNullOrEmpty(parentSourceID))
                            {
                                jsonFields.Add("ParentSourceID", parentSourceID);
                            }

                            for (int i = 0; i < targetAttributeTypeFields.Count; i++)
                            {
                                jsonFields.Add(targetAttributeTypeFields[i].Name, xls.GetCellValueAsString(currentRowIndex, i + currentColumnIndex + 1));
                            }

                            models.Add(jsonFields);

                            currentRowNumber++;    // This number must be unique.  A unique number per fusion attribute.
                            currentRowIndex++;
                        }

                        #endregion

                        #region Save to queue for processing

                        var import = new BulkFusionImport { Models = models, Relationships = new FusionRelationshipModels() };

                        var json = JsonConvert.SerializeObject(import);

                        var dateString = DateTime.UtcNow.ToString("yyyy-MM-dd_hh.mm.ss");

                        var folder = string.Format("bulk-fusion-{0}", Company.CurrentCompanyID);
                        Storage.CreateFolder(folder);
                        var fileName = $"{typeID}.{id}.{dateString}.json";
                        Storage.CreateFile(folder, fileName, json);

                        await Queue.CreateMessageAsync(Config.GetValue<string>("FusionLoadQueue"), new FusionProcessingData
                        {
                            CompanyID = Company.CurrentCompanyID,
                            FusionID = id,
                            LogFileName = fileName
                        });

                        #endregion
                    }
                }
                
                return new HttpStatusCodeResult(HttpStatusCode.OK, "File uploaded and queued for processing.");
            }
            catch (BaseException ex)
            {
                SendException(ex, new Dictionary<string, string>() { 
                            { "FusionID", id.ToString() },
                            { "FusionAttributeTypeID", attributeTypeID.ToString() } 
                });
                return new HttpStatusCodeResult(ex.StatusCode, ex.StatusDescription); //jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex, new Dictionary<string, string>() { 
                            { "FusionID", id.ToString() },
                            { "FusionAttributeTypeID", attributeTypeID.ToString() } 
                });
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);//jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Json

        string parentSql = @"
with h as	(
			select	A.ObjectID as ID,
					P.ParentID,
                    A.Name,
					0 as [Level]
			from	AssetType A
					outer apply	(
								select	SubjectID as ParentID
								from	IntersectType I
										inner join [Predicate] P on P.ID = I.PredicateID and P.Type = 3 and I.Subject = A.Object and I.Object = A.Object and I.ObjectID = A.ObjectID 
					) P
			where	A.Object = 'FusionAttributeType' and A.ObjectID = @t

			union all

			select	A.ObjectID as ID,
					P.ParentID,
                    A.Name,
					C.[Level] + 1 as [Level]
			from	AssetType A
					inner join h as C on A.Object = 'FusionAttributeType' and A.ObjectID = C.ParentID
					outer apply	(
								select	SubjectID as ParentID
								from	IntersectType I
										inner join [Predicate] P on P.ID = I.PredicateID and P.Type = 3 and I.Subject = A.Object and I.Object = A.Object and I.ObjectID = A.ObjectID 
					) P
			)

select * from h where ID <> @t order by h.[Level] desc;";

        //THIS IS LOOKING AT NEW INTERSECT LOGIC. TO BE ADDED IN LATER RELEASE.
        //        string parentSql = @"
        //with h as	(
        //			select	I.ID as IntersectTypeID,
        //					I.SubjectID,
        //					I.ObjectID,
        //                    A.Name,
        //					0 as [Level]
        //			from	AssetType A
        //					left join IntersectTypeDetail I on I.Object = 'FusionAttributeType' and I.ObjectID = @t and I.PredicateType = 3
        //			where	A.Object = 'FusionAttributeType' and A.ObjectID = @t

        //			union all

        //			select	I.ID as IntersectTypeID,
        //					I.SubjectID,
        //					I.ObjectID,
        //                    A.Name,
        //					C.[Level] + 1 as [Level]
        //			from	AssetType A
        //					inner join h as C on A.Object = 'FusionAttributeType' and A.ObjectID = C.SubjectID 
        //					outer apply (
        //						select	ID, SubjectID, ObjectID
        //						from	IntersectTypeDetail 
        //						where	Object = 'FusionAttributeType' and ObjectID = A.ObjectID and PredicateType = 3
        //					) I
        //			)

        //select * from h order by h.[Level] asc;";

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

            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
            var pluralFusionAttributeName = pluralize.Pluralize(fusionAttributeTypeName);

            var sqlFieldModels = new List<SqlFieldModel>();

            #region Parents

            var parents = Company.Query<dynamic>(parentSql, new { t = fusionAttributeTypeID }).ToList();

            //Parent columns have be listed in ASC order by Level.
            parents.ForEach(i =>
            {
                var thisJoin = ((int)i.Level == 1) ?
                    $" inner join FusionAttribute P{i.Level} on P{i.Level}.ID = A.ParentID" :
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

            //THIS IS LOOKING AT NEW INTERSECT LOGIC. TO BE ADDED IN LATER RELEASE.
            //for (var i = 0; i < parents.Count; i++)
            //{
            //    var intersectTypeID = (int?)parents[i].IntersectTypeID;
            //    var level = (int)parents[i].Level;

            //    if (intersectTypeID.HasValue)
            //    {
            //        var columnName = parents[i+1].Name;
            //        var subjectID = (int)parents[i].SubjectID;
            //        var objectID = (int)parents[i].ObjectID;

            //        var thisJoin = $" inner join [Intersect] P{level} on P{level}.Subject = 'FusionAttribute' and P{level}.Object = 'FusionAttribute'" +
            //            $" inner join IntersectType PT{level} on PT{level}.ID = P{level}.IntersectTypeID and PT{level}.SubjectID = {subjectID} and PT{level}.ObjectID = {objectID}" +
            //            $" inner join FusionAttribute PF{level} on PF{level}.ID = P{level}.SubjectID and P{level}.ObjectID = " +
            //            ((level == 0) ? $"A.ID" : $"P{level - 1}.SubjectID");

            //        sqlFieldModels.Add(new SqlFieldModel
            //        {
            //            FieldColumnName = $"Parent{subjectID}",
            //            FieldName = $"PF{level}.Name as Parent{subjectID}",
            //            FieldFriendlyName = columnName,
            //            FieldJoin = thisJoin,
            //            JoinOrder = -level,

            //        });
            //    }
            //}

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
            //document.FreezePanes(1, col);
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
            var hasEditRights = Company.HasPermission(SystemObjects.Fusion, fusionID, core.enums.Claim.Update);
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

            //Parent joins have be listed in ASC order by Level.
            var parentQueryJoinText = "";
            parents.OrderBy(i => i.Level).ToList().ForEach(i =>
            {
                var thisJoin = ((int)i.Level == 1) ?
                    $" inner join FusionAttribute P{i.Level} on P{i.Level}.ID = A.ParentID" :
                    $" inner join FusionAttribute P{i.Level} on P{i.Level}.ID = P{i.Level - 1}.ParentID";

                parentQueryJoinText += thisJoin;
            });

            //THIS IS LOOKING AT NEW INTERSECT LOGIC. TO BE ADDED IN LATER RELEASE.
            //var parentFilterColumnText = "";
            //var parentQueryColumnText = "";
            //var parentQueryJoinText = "";

            //var parentFilterPresent = parents.Count > 1 && filterFields.Any(i => i.StartsWith("Parent"));
            //for (var i = 0; i < parents.Count; i++)
            //{
            //    var intersectTypeID = (int?)parents[i].IntersectTypeID;
            //    var level = (int)parents[i].Level;

            //    if (intersectTypeID.HasValue)
            //    {
            //        var columnName = parents[i + 1].Name;
            //        var subjectID = (int)parents[i].SubjectID;
            //        var objectID = (int)parents[i].ObjectID;

            //        var thisJoin = $" inner join [Intersect] P{level} on P{level}.Subject = 'FusionAttribute' and P{level}.Object = 'FusionAttribute'" +
            //            $" inner join IntersectType PT{level} on PT{level}.ID = P{level}.IntersectTypeID and PT{level}.SubjectID = {subjectID} and PT{level}.ObjectID = {objectID}" +
            //            $" inner join FusionAttribute PF{level} on PF{level}.ID = P{level}.SubjectID and P{level}.ObjectID = " +
            //            ((level == 0) ? $"A.ID" : $"P{level - 1}.SubjectID");

            //        parentQueryJoinText += thisJoin;

            //        var thisColumn = $", PF{level}.Name as Parent{subjectID}";
            //        parentQueryColumnText += thisColumn;
            //        if (parentFilterPresent)
            //        {
            //            parentFilterColumnText += thisColumn;
            //        }
            //        else
            //        {
            //            if (filterFields.Contains($"Parent{subjectID}"))
            //            {
            //                parentFilterColumnText += thisColumn;
            //            }
            //        }
            //    }
            //}

            #endregion

            if (columns.Contains("[type]"))
                columns = columns.Replace("[type]", "[_type]");

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("f", fusionID);
            dbArgs.Add("t", fusionAttributeTypeID);

            #region Count SQL

            var countSql = $@"
select  A.ID, 
        A.Name, 
        A.FusionAttributeTypeID,
        'FusionAttribute' as [Type]
        {filtercolumns} 
        {parentFilterColumnText} 
from	FusionAttribute A {parentQueryJoinText} {filterjoins}
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
        {columns} 
        {parentQueryColumnText} 
from	FusionAttribute A {parentQueryJoinText} {joins} 
where A.FusionID = @f and A.FusionAttributeTypeID = @t and A.Deleted = 0";

            sql = $@"select * from ({sql}) A";
            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder);
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

            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
            var pluralFusionQueryAttributeTypeName = pluralize.Pluralize(fusionQueryAttributeTypeName);

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
            var totalColumns = sqlFieldModels.Count;

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
	                FT.FriendlyName as FriendlyName
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
                    Value = item.FormattedValue
                });
            }
                        
            model["Fields"] = res;            

            return Json(model, JsonRequestBehavior.AllowGet);
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
            select	',' + IA.Name  AS [text()]
            from	FusionOwner [IO]
					inner join Artifact IA on IA.ID = [IO].ArtifactID and [IO].FusionID = A.ID
            ORDER BY IA.Name
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
