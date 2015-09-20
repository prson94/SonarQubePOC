using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;
using d360.core;
using System.Data.Entity.Design.PluralizationServices;
using Newtonsoft.Json;
using System.Diagnostics;
using d360.extensions;
using System.Text;
using d360.web.Models.Attributes;
using SpreadsheetLight;
using System.IO;
using System.Xml.Linq;
using System;
using System.Text.RegularExpressions;
using d360.core.exceptions;
using System.Net;

namespace d360.web.Controllers
{
    [RoutePrefix("fusion"), Authorize]
    public class FusionController : BaseController
    {
        #region DI

        IStorageProvider Storage;

        public FusionController(CommunityContext community, CompanyContext company, IStorageProvider storage)
            : base(community, company)
        {
            Storage = storage;
        }

        #endregion

        private static Regex _invalidXMLChars = new Regex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled);

        #region Partials

        [Route("{typeID:int}/configurations/{id:int}/ownership")]
        public ActionResult Ownership(int typeID, int id)
        {
            ViewData.Add("FusionTypeID", typeID);
            ViewData.Add("FusionID", id);

            return PartialView();
        }

        [Route("{typeID:int}/configurations/{id:int}/promotion")]
        public ActionResult Promotion(int typeID, int id)
        {
            ViewData.Add("FusionTypeID", typeID);
            ViewData.Add("FusionID", id);

            return PartialView();
        }

        public ActionResult FusionExecution(int id)
        {
            return PartialView(new ObjectModel { ObjectType = "FusionExecution", ObjectID = id });
        }

        public ActionResult FusionExecutionRawLog(int id)
        {
            //var execution = Company.Query<dynamic>(@"select * from fusion.Execution where ID = @id", new { id = id }).SingleOrDefault();
            //if (execution == null) return HttpNotFound();

            //var url = "";
            //if (!string.IsNullOrEmpty(execution.RawLogFileName))
            //    url = Storage.GetFileSecureUrl(string.Format("bulk-fusion-{0}", Company.CurrentCompanyID), execution.RawLogFileName);

            //ViewBag.LogFileUrl = url;

            return PartialView();
        }

        public ContentResult _FusionExecutionRawLog(int id)
        {
            var execution = Company.Query<dynamic>(@"select * from fusion.Execution where ID = @id", new { id = id }).SingleOrDefault();

            var bytes = Storage.GetFileAsBytes(string.Format("bulk-fusion-{0}", Company.CurrentCompanyID), execution.RawLogFileName);
            return Content(Encoding.UTF8.GetString(bytes), "application/json");
        }

        public ActionResult RelationshipAggregatesOverlay(SystemObjects type, int id, SystemObjects targetType, int targetID, int parentAttributeID = 0)
        {
            var dtl = Company.GetObjectDetail(type, id);
            ViewBag.Title = (dtl != null) ? string.Format("Relationships for {0}", dtl.Name) : "Relationships";
            dtl = null;

            ViewBag.Type = type.ToString();
            ViewBag.ID = id;
            
            ViewBag.TargetType = targetType.ToString();
            ViewBag.TargetID = targetID;

            ViewBag.ParentAttributeID = parentAttributeID;

            return PartialView();
        }

        //void AddTemplateSheetForAttributeTypesLevel(SLDocument document, List<FusionAttributeType> types, List<FieldTypeWithRelation> fields, int? parentID)
        //{
        //    var requiredStyle = new SLStyle{ Font = new SLFont { Bold = true, FontColor = System.Drawing.Color.Red } };

        //    var parentName = "";
        //    if (parentID.HasValue)
        //    {
        //        parentName  = types.Single(i => i.ID == parentID).Name + " ";
        //    }

        //    foreach (var t in types.Where(i => i.ParentID == parentID).OrderBy(i => i.Name))
        //    {
        //        document.AddWorksheet(string.Format("{0}{1}", parentName, t.Name));

        //        var theseFields = fields.Where(i => i.ObjectID == t.ID).OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToList();

        //        document.SetCellValue(1, 1, "SourceID");
        //        document.SetCellStyle(1, 1, requiredStyle);
        //        var sourceIDComment = document.CreateComment();
        //        sourceIDComment.SetText("This value must be unique across all entries for this fusion instance.  It is usually a set of values that defines the heirarchy for this item, separated by a delimiter.  For example, a column might by: schemaName.tableName.columnName");
        //        sourceIDComment.AutoSize = true;
        //        document.InsertComment(1, 1, sourceIDComment);

        //        //document.SetCellValue(2, 1, "=CONCATENATE(B1)");

        //        var pushValue = 2;

        //        if (parentID.HasValue)
        //        {
        //            pushValue = 3;
        //            document.SetCellValue(1, 2, "ParentSourceID");
        //            document.SetCellStyle(1, 2, requiredStyle);

        //            var parentSourceIDComment = document.CreateComment();
        //            parentSourceIDComment.SetText("The SourceID for this item's parent. It is usually a set of values that defines the heirarchy for this item, separated by a delimiter.  For example, a column's parent is a table that might have a SourceID like: schemaName.tableName");
        //            parentSourceIDComment.AutoSize = true;
        //            document.InsertComment(1, 2, parentSourceIDComment);
        //        }

        //        document.SetCellValue(1, pushValue, "Name");
        //        document.SetCellStyle(1, pushValue, requiredStyle);
        //        pushValue++;

        //        for (int i = 0; i < theseFields.Count; i++)
        //        {
        //            document.SetCellValue(1, i + pushValue, theseFields[i].Name);
        //            if (theseFields[i].IsRequired)
        //            {
        //                document.SetCellStyle(1, i + pushValue, requiredStyle);
        //            }
        //        }

        //        AddTemplateSheetForAttributeTypesLevel(document, types, fields, t.ID);
        //    }
        //}

        //[Route("{typeID:int}/configurations/{id:int}/template"), FileDownload, HttpGet]
        //public FileResult GetFusionManualLoadTemplate(int typeID, int id)
        //{
        //    var fusion = Company.GetById<Fusion>(id, i => i.FusionType);

        //    var document = new SLDocument();

        //    var fusionAttributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == fusion.FusionTypeID).ToList();
        //    var fusionAttributeTypeIDs = fusionAttributeTypes.Select(i => i.ID).ToList();
        //    var fusionAttributeTypeFields = Company.Filter<FieldTypeWithRelation>(i => i.ObjectType == "FusionAttributeType" && fusionAttributeTypeIDs.Contains(i.ObjectID)).ToList();

        //    AddTemplateSheetForAttributeTypesLevel(document, fusionAttributeTypes, fusionAttributeTypeFields, null);

        //    document.DeleteWorksheet("Sheet1");

        //    #region Add Relationship Worksheet to end

        //    document.AddWorksheet("Relationships");
        //    document.SetCellValue(1, 1, "Side 1 SourceID");
        //    document.SetCellValue(1, 2, "Side 2 SourceID");

        //    #endregion


        //    var stream = new MemoryStream();
        //    document.SaveAs(stream);
        //    return File(stream.ToArray(), "application/vnd.ms-excel", string.Format("Load Template for {0}.xls", fusion.FusionType.Name));
        //}


        [Route("{typeID:int}/configurations/{id:int}/template/{attributeTypeID:int}"), FileDownload, HttpGet]
        public FileResult GetFusionManualLoadTemplateForAttributeType(int typeID, int id, int attributeTypeID)
        {
            var fusion = Company.GetById<Fusion>(id, i => i.FusionType.FusionAttributeTypes);

            var document = new SLDocument();
            var requiredStyle = new SLStyle { Font = new SLFont { Bold = true } };

            var targetAttributeType = fusion.FusionType.FusionAttributeTypes.SingleOrDefault(i => i.ID == attributeTypeID);
            var targetAttributeTypeFields = Company.Filter<FieldTypeWithRelation>(i => i.Object == "FusionAttributeType" && i.ObjectID == attributeTypeID).ToList();
            
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
            return File(stream.ToArray(), "application/vnd.ms-excel", string.Format("Load Template for {0}.xls", fusion.FusionType.Name));
        }

        //void ParseNodesInTemplateSheetForAttributeTypesLevel(SLDocument document, XElement root, List<FusionAttributeType> types, List<FieldTypeWithRelation> fields, int rowNumber, int? parentID)
        //{
        //    var parentName = "";
        //    if (parentID.HasValue) parentName = types.Single(i => i.ID == parentID).Name + " ";

        //    foreach (var t in types.Where(i => i.ParentID == parentID).OrderBy(i => i.Name))
        //    {
        //        if (document.SelectWorksheet(string.Format("{0}{1}", parentName, t.Name)))
        //        {
        //            #region Get fields to find

        //            var namesToFind = new Dictionary<string, int>();
                    
        //            namesToFind.Add("SourceID", 1);

        //            var pushValue = 2;

        //            if (parentID.HasValue)
        //            {
        //                namesToFind.Add("ParentSourceID", 2);
        //                pushValue = 3;
        //            }
                    
        //            namesToFind.Add("Name", pushValue);
        //            pushValue++;

        //            var theseFields = fields.Where(i => i.ObjectID == t.ID).OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToList();
        //            for (int i = 0; i < theseFields.Count; i++)
        //            {
        //                namesToFind.Add(theseFields[i].Name, i + pushValue);
        //            }

        //            #endregion

        //            #region Parse rows in sheet

        //            try
        //            {
        //                var stats = document.GetWorksheetStatistics();
        //                var endRowIndex = stats.EndRowIndex;
        //                var rowIndex = 2;

        //                while (rowIndex <= endRowIndex)
        //                {
        //                    var node = new XElement("m", new XAttribute("id", rowNumber));

        //                    foreach (var k in namesToFind.Keys)
        //                    {
        //                        var value = document.GetCellValueAsString(rowIndex, namesToFind[k]);
        //                        if (string.IsNullOrEmpty(value)) value = "";

        //                        var childNodeName = _invalidXMLChars.Replace(k, "");

        //                        if (value.Contains("<") || value.Contains(">"))
        //                            node.Add(new XElement(childNodeName, new XCData(value)));
        //                        else
        //                        {
        //                            node.Add(new XElement(childNodeName, _invalidXMLChars.Replace(value, "")));
        //                        }
        //                    }
        //                    node.Add(new XElement("FusionAttributeTypeID", t.ID));

        //                    root.Add(node);
        //                    rowIndex++;
        //                    rowNumber++;    //This number must be unique across all tabs.  A unique number per fusion attribute.
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //            }

        //            #endregion

        //        }

        //        ParseNodesInTemplateSheetForAttributeTypesLevel(document, root, types, fields, rowNumber, t.ID);
        //    }
        //}

        internal class GenericFusionAttribute
        {
            public string SourceID { get; set; }
            public string ParentSourceID { get; set; }
            public int FusionAttributeTypeID { get; set; }
            public string Name { get; set; }
        }

        [Route("{typeID:int}/configurations/{id:int}/template/{attributeTypeID:int}"), HttpPost]
        public HttpStatusCodeResult UploadFusionManualLoad(int typeID, int id, int attributeTypeID)
        {
            try
            {
                var file = Request.Files[0];
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
                var targetAttributeTypeFields = Company.Filter<FieldTypeWithRelation>(i => i.Object == "FusionAttributeType" && i.ObjectID == attributeTypeID).ToList();

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

                                var node = new XElement("m", new XAttribute("id", currentRowNumber));

                                node.Add(new XElement("Name", xls.GetCellValueAsString(currentRowIndex, currentColumnIndex)));
                                node.Add(new XElement("FusionAttributeTypeID", nodeID));
                                node.Add(new XElement("SourceID", sourceID));
                                if (!string.IsNullOrEmpty(parentSourceID))
                                {
                                    node.Add(new XElement("ParentSourceID", parentSourceID));
                                }

                                mXml.Add(node);        // Add node.
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

                        var node = new XElement("m", new XAttribute("id", currentRowNumber));

                        node.Add(new XElement("Name", xls.GetCellValueAsString(currentRowIndex, currentColumnIndex)));
                        node.Add(new XElement("FusionAttributeTypeID", attributeTypeID));
                        node.Add(new XElement("SourceID", sourceID));
                        if (!string.IsNullOrEmpty(parentSourceID))
                        {
                            node.Add(new XElement("ParentSourceID", parentSourceID));
                        }

                        for (int i = 0; i < targetAttributeTypeFields.Count; i++)
                        {
                            node.Add(new XElement(targetAttributeTypeFields[i].Name, xls.GetCellValueAsString(currentRowIndex, i + currentColumnIndex + 1)));
                        }

                        mXml.Add(node);        // Add node.

                        currentRowNumber++;    // This number must be unique.  A unique number per fusion attribute.
                        currentRowIndex++;
                    }

                    #endregion

                    #region Save to queue for processing

                    var doc = new XElement("import", mXml, new XElement("rs"));
                    Company.AddFusionQueueItem(new QueueFusionItem { FusionID = id, Data = doc.ToString() });

                    #endregion                
                }

                return new HttpStatusCodeResult(HttpStatusCode.Created, "File uploaded and queued for processing.");
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

        public JsonResult RelationshipAggregates(SystemObjects type, int id, int parentAttributeID = 0)
        {
            var sql = "";
            IEnumerable<RelationshipAggregate> models = null;
            switch (type) 
            {
                case SystemObjects.Fusion:
                    #region
                    sql =
@"SELECT    'Fusion' as ObjectType, 
		    @id as ObjectID, 
		    TD.Name as TypeName, 
		    TD.ObjectID as TypeID, 
		    TD.[Object] as Type, 
		    TD.IconBackColor, 
		    TD.IconForeColor, 
		    TD.IconText, 
		    COALESCE(N.[Count], 0) as [Count]
FROM	    (
		    select	Count(1) as [Count], TN.ObjectType as T, TN.ObjectID as I
		    from	FusionAttribute FA
				    inner join cache.Relationships R on FA.FusionID = @id and R.SourceObject = 'FusionAttribute' and R.SourceObjectID = FA.ID
				    inner join IntersectTypeNode TN on TN.ID = R.TargetIntersectTypeNodeID 
		    group by	TN.ObjectType, TN.ObjectID
		    ) N
		    inner join cache.ObjectDetails TD on TD.[Object] = N.T and TD.ObjectID = N.I
            order by TD.Name";
                    models = Company.Query<RelationshipAggregate>(sql, new { id = id });
                    break;
                    #endregion
                case SystemObjects.FusionAttribute:
                    #region
                    sql =
@"with h as	(
			select	ID,
					ParentID
			from	FusionAttribute
			where	ID = @id
			union all
			select	C.ID,
					C.ParentID
			from	FusionAttribute C
					inner join h as P on P.ID = C.ParentID
			)

SELECT    'FusionAttribute' as ObjectType, 
            @id as ObjectID, 
            TD.Name as TypeName, 
            TD.ObjectID as TypeID, 
            TD.[Object] as Type, 
            TD.IconBackColor, 
            TD.IconForeColor, 
            TD.IconText, 
            COALESCE(N.[Count], 0) as [Count]
FROM		(
			select	Count(1) as [Count], TN.ObjectType as T, TN.ObjectID as I
			from	h
					inner join  cache.Relationships R on R.SourceObject = 'FusionAttribute' and R.SourceObjectID = h.ID
                    inner join IntersectTypeNode TN on TN.ID = R.TargetIntersectTypeNodeID 
			group by	TN.ObjectType, TN.ObjectID
			) N
			inner join cache.ObjectDetails TD on TD.[Object] = N.T and TD.ObjectID = N.I
            order by TD.Name";
                    models = Company.Query<RelationshipAggregate>(sql, new { id = id });
                    break;
                    #endregion
                case SystemObjects.FusionAttributeType:
                    #region
                    sql =
@"with h as	(
			select	ID,
					ParentID
			from	FusionAttribute
			where	FusionAttributeTypeID = @id and ( (ParentID = @parentID and @parentID > 0) OR (@parentID = 0 and 1=1) )
			union all
			select	C.ID,
					C.ParentID
			from	FusionAttribute C
					inner join h as P on P.ID = C.ParentID
			)

SELECT		'FusionAttributeType' as ObjectType, 
		    @id as ObjectID, 
		    TD.Name as TypeName, 
		    TD.ObjectID as TypeID, 
		    TD.[Object] as Type, 
		    TD.IconBackColor, 
		    TD.IconForeColor, 
		    TD.IconText, 
		    COALESCE(N.[Count], 0) as [Count]
FROM	    (
			select	Count(1) as [Count], TN.ObjectType as T, TN.ObjectID as I
			from	FusionAttribute FA
					inner join  h on h.ID = FA.ID
					inner join cache.Relationships R on R.SourceObject = 'FusionAttribute' and R.SourceObjectID = FA.ID
					inner join IntersectTypeNode TN on TN.ID = R.TargetIntersectTypeNodeID 
			group by	TN.ObjectType, TN.ObjectID
			) N
		    inner join cache.ObjectDetails TD on TD.[Object] = N.T and TD.ObjectID = N.I
            order by TD.Name";
                    models = Company.Query<RelationshipAggregate>(sql, new { id = id, parentID = parentAttributeID });
                    break;
                    #endregion
            }

            return Json(
                models,
                JsonRequestBehavior.AllowGet);
        }

        public JsonNetResult TreeNodes(int typeID, int fusionID)
        {
            var types = Company.Query<dynamic>(@"
with th as	(
			select	A.ID,
					A.ParentID,
					A.Name
			from	FusionAttributeType A
			where	A.FusionTypeID = @id and A.ParentID is null
			union all
			select	A.ID,
					A.ParentID,
					A.Name
			from	FusionAttributeType A
					inner join th P on P.ID = A.ParentID
			)

select * from th", new { id = typeID });

            var attributes = Company.Query<dynamic>(@"with h as	(
			select	A.ID,
					A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
			from	FusionAttribute A
			where	A.FusionID = @id and A.ParentID is null
			union all
			select	A.ID,
					A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
			from	FusionAttribute A
					inner join h P on P.ID = A.ParentID
			where	A.FusionAttributeTypeID in (select ParentID from FusionAttributeType)
			)
select	*
from	h", new { id = fusionID });

            return new JsonNetResult { Data = new { Types = types, Attributes = attributes }, Formatting = Formatting.None };
        }

        public JsonNetResult ItemsByParent(int fusionTypeID, int fusionID, SystemObjects parentType, int? parentID, int? parentFusionAttributeTypeID, int? parentFusionAttributeID, string sortDataField, string sortOrder, int pagenum, int pagesize)
        {
            string sql = "";
            int total = 0;
            IEnumerable<dynamic> query = null;

            switch (parentType)
            {
                case SystemObjects.FusionAttribute:
                    #region

                    var joins = "";
                    var columns = "";
                    var intersectSql = "";
                    var intersects = new List<int>();

                    if (parentFusionAttributeTypeID.HasValue)
                    {
                        getDynamicFieldJoinStatements(parentFusionAttributeTypeID.Value, "FusionAttribute", out joins, out columns, false);

                        intersectSql = string.Format(@"select IntersectTypeID from utility.RelationshipTypes where SourceObjectType = 'FusionAttributeType' and SourceObjectID = {0}", parentFusionAttributeTypeID.Value);
                        intersects = Company.Query<int>(intersectSql).ToList();
                    }

                    var intersectQueryColumnText = "";
                    var intersectQueryPivotText = "";

                    intersects.ForEach(i =>
                    {
                        if (!string.IsNullOrEmpty(intersectQueryColumnText)) intersectQueryColumnText += ", ";
                        if (!string.IsNullOrEmpty(intersectQueryPivotText)) intersectQueryPivotText += ", ";

                        intersectQueryColumnText += string.Format("P.[IntersectType{0}]", i);
                        intersectQueryPivotText += string.Format("[IntersectType{0}]", i);
                    });

                    if (string.IsNullOrEmpty(intersectQueryColumnText)) intersectQueryColumnText = "P.[IntersectType0]";
                    if (string.IsNullOrEmpty(intersectQueryPivotText)) intersectQueryPivotText = "[IntersectType0]";


                    var querySql = string.Format(
@"select A.ID, A.Name, 
A.FusionAttributeTypeID,
'FusionAttribute' as [Type],
{0} RT.*
from	FusionAttribute A {1} 
outer apply (
			select	{2}
			from	(
					select	'IntersectType' + cast(RT.IntersectTypeID as varchar(10)) as [IntersectType],
							count(R.IntersectTypeID) as [Count]
					from	(
							select	IntersectTypeID 
							from	utility.RelationshipTypes 
							where	SourceObjectType = 'FusionAttributeType'
									and SourceObjectID = A.FusionAttributeTypeID
							) RT
							left join cache.Relationships R on R.IntersectTypeID = RT.IntersectTypeID 
																and R.SourceObject = 'FusionAttribute'
																and R.SourceObjectID = A.ID
					group by 'IntersectType' + cast(RT.IntersectTypeID as varchar(10))
					) as I
			pivot	(
					min([Count]) for [IntersectType] in ({3})
					) as P
			) RT
where A.FusionID = @f and A.FusionAttributeTypeID = @t {4} and A.Deleted = 0", columns, joins, intersectQueryColumnText, intersectQueryPivotText, (parentID.HasValue ? "and A.ParentID = @p" : ""));

                    var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
                    sql = string.Format(@"select * from ({0}) A", querySql);

                    countSql = applyFilteringSuffix(countSql, Request);
                    total = Company.Query<int>(countSql, new { f = fusionID, t = parentFusionAttributeTypeID, p = parentID }).First();

                    sql = applyFilteringSuffix(sql, Request);
                    sql = applySortSuffix(sql, sortDataField, sortOrder);
                    sql = applyPagingSuffix(sql, pagenum, pagesize);

                    query = Company.Query<dynamic>(sql, new { f = fusionID, t = parentFusionAttributeTypeID, p = parentID });
                    
                    #endregion
                    break;
                case SystemObjects.FusionAttributeType:
                    #region
                    sql =
@"select	T.ID,
			T.Name,
			C.IsLeaf,
            'FusionAttributeType' as [Type],
            @p as ParentFusionAttributeID
from	    FusionAttributeType T
			cross apply (
						SELECT	case 
									when COUNT(1) > 0 then CAST(0 as bit) 
									else 1
								end as IsLeaf 
						FROM	FusionAttributeType 
						where	ParentID = T.ID
						) C
	where	T.FusionTypeID = @t";
                    sql += (parentID.HasValue) ? " and T.ParentID = @pt" : " and T.ParentID is null";
                    sql += " order by T.Name";

                    query = Company.Query<dynamic>(sql, new { t = fusionTypeID, pt = parentID, p = parentFusionAttributeID });
                    total = query.Count();
                    #endregion
                    break;
            }

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Gets fusion attribute types based on the given fusion type and possible parent attribute type
        /// </summary>
        /// <param name="t">The fusion type ID</param>
        /// <param name="p">The parent fusion attribute type (optional)</param>
        /// <returns>A list of fusion attributes types.</returns>
        public JsonNetResult _AttributeTypesByParentType(int t, int? p)
        {
            Trace.TraceInformation("Calling FusionController._AttributeTypesByParentType : t={0};p={1}", t, p);

            string sql = 
@"select	T.ID,
			T.Name,
			C.IsLeaf
from	    FusionAttributeType T
			cross apply (
						SELECT	case 
									when COUNT(1) > 0 then CAST(0 as bit) 
									else 1
								end as IsLeaf 
						FROM	FusionAttributeType 
						where	ParentID = T.ID
						) C
	where	T.FusionTypeID = @t";
            sql += (p.HasValue) ? " and T.ParentID = @p" : " and T.ParentID is null";
            sql +=  " order by T.Name";

            var query = Company.Query<dynamic>(sql, new { t = t, p = p });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Gets fusion attribute types based on the given fusion type and possible parent attribute type
        /// </summary>
        /// <param name="t">The fusion ID</param>
        /// <param name="p">The parent fusion attribute type (optional)</param>
        /// <returns>A list of fusion attributes types.</returns>
        //public JsonNetResult _AttributesByParentType(int f, int at, int? p)
        //{
        //    Trace.TraceInformation("Calling FusionController._AttributesByParentType : f={0};at={1};p={2}", f, at, p);

        //    string sql = "select ID, Name from FusionAttribute where FusionID = @f and FusionAttributeTypeID = @at";
        //    if (p.HasValue) sql += " and ParentID = @p";
        //    sql +=  " order by Name";

        //    var query = Company.Query<dynamic>(sql, new { f = f, at = at, p = p });

        //    return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        //}

//        public JsonNetResult _ChildItemsByParent(int fusionID, int? p, int t, string sortDataField, string sortOrder, int pagenum, int pagesize)
//        {
//            Trace.TraceInformation("Calling FusionController._ChildItemsByParent : p={0},t={1}", p, t);

//            var joins = "";
//            var columns = "";
//            getDynamicFieldJoinStatements(t, "FusionAttribute", out joins, out columns, false);

//            var intersectSql = string.Format(@"select IntersectTypeID from utility.RelationshipTypes where SourceObjectType = 'FusionAttributeType' and SourceObjectID = {0}", t);
//            var intersects = Company.Query<int>(intersectSql).ToList();
//            var intersectQueryColumnText = "";
//            var intersectQueryPivotText = "";

//            intersects.ForEach(i =>
//            {
//                if (!string.IsNullOrEmpty(intersectQueryColumnText)) intersectQueryColumnText += ", ";
//                if (!string.IsNullOrEmpty(intersectQueryPivotText)) intersectQueryPivotText += ", ";

//                intersectQueryColumnText += string.Format("P.[IntersectType{0}]", i);
//                intersectQueryPivotText += string.Format("[IntersectType{0}]", i);
//            });

//            if (string.IsNullOrEmpty(intersectQueryColumnText)) intersectQueryColumnText = "P.[IntersectType0]";
//            if (string.IsNullOrEmpty(intersectQueryPivotText)) intersectQueryPivotText = "[IntersectType0]";


//            var querySql = string.Format(@"select A.ID, A.Name, {0} RT.*
//from	FusionAttribute A {1} 
//outer apply (
//			select	{2}
//			from	(
//					select	'IntersectType' + cast(RT.IntersectTypeID as varchar(10)) as [IntersectType],
//							count(R.IntersectTypeID) as [Count]
//					from	(
//							select	IntersectTypeID 
//							from	utility.RelationshipTypes 
//							where	SourceObjectType = 'FusionAttributeType'
//									and SourceObjectID = A.FusionAttributeTypeID
//							) RT
//							left join cache.Relationships R on R.IntersectTypeID = RT.IntersectTypeID 
//																and R.SourceObject = 'FusionAttribute'
//																and R.SourceObjectID = A.ID
//					group by 'IntersectType' + cast(RT.IntersectTypeID as varchar(10))
//					) as I
//			pivot	(
//					min([Count]) for [IntersectType] in ({3})
//					) as P
//			) RT
//where A.FusionID = @f and A.FusionAttributeTypeID = @t {4} and A.Deleted = 0", columns, joins, intersectQueryColumnText, intersectQueryPivotText, (p.HasValue ? "and A.ParentID = @p" : ""));

//            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
//            var sql = string.Format(@"select * from ({0}) A", querySql);

//            countSql = applyFilteringSuffix(countSql, Request);
//            int total = Company.Query<int>(countSql, new { f = fusionID, t = t, p = p }).First();

//            sql = applyFilteringSuffix(sql, Request);
//            sql = applySortSuffix(sql, sortDataField, sortOrder);
//            sql = applyPagingSuffix(sql, pagenum, pagesize);

//            var query = Company.Query<dynamic>(sql, new { f = fusionID, t = t, p = p });

//            return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
//        }

        public JsonResult GetFusionRuleStatistics(int id)
        {
            var model = new
            {
                OwnershipRuleCount = Company.FusionAttributeOwnerRules.Count(i => i.FusionID == id),
                PromotionRuleCount = Company.FusionAttributePromotionRules.Count(i => i.FusionID == id)
            };

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
