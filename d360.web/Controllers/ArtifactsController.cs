using d360.core.entities;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/artifacts"), Authorize, AiHandleError]
    public class ArtifactsController : BaseController
    {
        #region DI

        public ArtifactsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Exports

        [Route("download/excel/{id:int}.xls"), FileDownload, HttpGet]
        public FileResult ToExcel(int id, string sortDataField, string sortOrder, string filter, string ownerUsers = "", string ownerGroups = "", bool listableOnly = true)
        { 
            var joins = "";
            var columns = "";

            var fields = getFieldTypesByObjectType("ArtifactType", id, listableOnly);

            getDynamicFieldJoinStatements(id, "Artifact", out joins, out columns, true, false, listableOnly, fields);

            var dbArgs = new Dapper.DynamicParameters();

            dbArgs.Add("id", id);

            joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups);

            #region Sql

            var sql = string.Format(@"
select	A.ID,
		A.Name,
		A.Description,
        A.ParentID,
		P.TextPath as Parent,
		A.TextPath,
		A.Status,
		V.Name as TaxonomyType,
        {0}
		dbo.GenerateNgObjectUrl('Artifact', A.ArtifactTypeID, A.ID) as Url
from	Artifact A inner join TaxonomyType V on (V.ID = A.TaxonomyTypeID)
        left join Artifact P on P.ID = A.ParentID 
        {1}
where A.ArtifactTypeID = @id and A.[Visible] = 1 ", columns, joins);

            #endregion

            //if simple filter specified add that citeria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                sql = $"{sql} and {addDynamicFieldSimpleFilter(new string[] { "A.Name", "A.Status", "V.Name", "A.TextPath" }, "Artifact", id, filter, dbArgs)}";
            }

            var type = Company.GetById<ArtifactType>(id);

            var document = new SLDocument();
            document.AddWorksheet("Items");

            sql = string.Format(@"select * from ({0}) A", sql);

            sql = applyFilteringSuffixBind(sql, Request, dbArgs, fields: fields);
            sql = applySortSuffix(sql, sortDataField, sortOrder, isNumericString: isSortColumnNumber(sortDataField, fields));

            fields = fields.Where(x => (x.Type != "FilteredLookup" && x.Type != "FusionLookup" && x.Type != "ComplexRelationLookup" && x.Type != "OwnershipLookup")).ToList();
                        
            var results = Company.Query<dynamic>(sql, dbArgs);
            var settings = Community.GetCompanySettings();

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Name");
            document.SetCellValue(1, index++, "Description");
            document.SetCellValue(1, index++, "TextPath");
            if(type.ParentID > 0)
            {
                document.SetCellValue(1, index++, "Parent");
            }
            foreach (var field in fields)
            {
                document.SetCellValue(1, index++, (string)field.FriendlyName);
            }

            document.SetCellValue(1, index++, settings["ArtifactType_TaxonomyTypeID"]);
            document.SetCellValue(1, index++, "Status");
            document.SetCellValue(1, index++, "Url");

            #endregion

            int rowNumber = 1;
            foreach (var row in results)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, (string)row.Name);
                document.SetCellValue(rowNumber, index++, (string)row.Description);
                document.SetCellValue(rowNumber, index++, (string)row.TextPath);
                if (type.ParentID > 0)
                {
                    document.SetCellValue(rowNumber, index++, (string)row.Parent);
                }

                foreach (var field in fields)
                {                    
                    switch ((field.Type ?? "").ToUpper())
                    {
                        case "DECIMAL":
                            double dVal = 0;
                            var decVal = (string)((row as IDictionary<string, object>)[$"Field{field.ID}"]);
                            if (double.TryParse(decVal, out dVal))
                                document.SetCellValue(rowNumber, index++, dVal);
                            else
                                document.SetCellValue(rowNumber, index++, decVal);                                                        
                            break;
                        case "NUMBER":
                            int intVal = 0;
                            var val = (string)((row as IDictionary<string, object>)[$"Field{field.ID}"]);
                            if (int.TryParse(val, out intVal))
                                document.SetCellValue(rowNumber, index++, intVal);
                            else
                                document.SetCellValue(rowNumber, index++, val);
                            break;
                        default:
                            document.SetCellValue(rowNumber, index++, (string)((row as IDictionary<string, object>)[$"Field{field.ID}"]));
                            break;
                    }
                    
                }

                document.SetCellValue(rowNumber, index++, (string)row.TaxonomyType);
                document.SetCellValue(rowNumber, index++, (string)row.Status);                
                document.SetCellValue(rowNumber, index++, (string)row.Url);
            }

            document.AutoFitColumn(1, index);
            
            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Filtered {type.Name} List for {DateTime.Now.ToShortDateString()}.xlsx");
        }

        #endregion

        #region Json

        [HttpGet, Route("artifactsbyparent"), NonNullableParameters]
        public JsonNetResult ArtifactsByParent(int parentID, int childArtifactTypeID, string sortDataField, string sortOrder, string filter, int pagenum = 0 , int pagesize = 20)
        {            
            return ByParent(parentID, sortDataField, sortOrder, filter, pagenum, pagesize, childArtifactTypeID);
        }

        [HttpPost, Route("byparent"), NonNullableParameters]
        public JsonNetResult ByParent(int parentID, string sortDataField, string sortOrder, string filter, int pagenum = 0, int pagesize = 20, int childArtifactTypeID = 0)
        {
            var d = new Dictionary<string, object>();
            d.Add("p", parentID);

            var sql = @"select	A.ID,
		A.Name,
		A.Description,
        A.ParentID,
		P.TextPath as Parent,
        dbo.GenerateObjectUrl('Artifact', P.ArtifactTypeID, P.ID) as ParentUrl,
		A.Status,
        A.DateLastCertified,
        {0}
		T.Name as TaxonomyType,
        A.TaxonomyTypeID,
        dbo.GenerateObjectUrl('Artifact', A.ArtifactTypeID, A.ID) as Url
from	Artifact A 
        inner join TaxonomyType T on T.ID = A.TaxonomyTypeID {1} 
        left join Artifact P on P.ID = A.ParentID 
        where A.ArtifactTypeID = @id and A.ParentID = @p and A.[Visible] = 1";
            var model = processDynamicResults(
                sql, Request,
                "ArtifactType", childArtifactTypeID,
                true,
                sortDataField, sortOrder, pagenum, pagesize,
                new string[] { "A.Name", "A.Status", "T.Name", "P.TextPath" },
                filter, extraParams: d, applyHiddenFilters: true, includeIdColumn: false);
            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("artifactsbytype"), NonNullableParameters]
        public JsonNetResult ArtifactsByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter, string ownerUsers = "", string ownerGroups = "")
        {
            return ByType(id, sortDataField, sortOrder, pagenum, pagesize, filter, ownerUsers, ownerGroups);
        }

        [HttpPost, Route("bytype"), NonNullableParameters]
        public JsonNetResult ByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter, string ownerUsers = "", string ownerGroups = "")
        {
            try
            {               
                var sql = @"select	A.ID,
		    A.Name,
		    A.Description,
            A.ParentID,
		    P.TextPath as Parent,
            dbo.GenerateObjectUrl('Artifact', P.ArtifactTypeID, P.ID) as ParentUrl,
		    A.Status,
            A.DateLastCertified,
            {0}
		    T.Name as TaxonomyType,
            A.TaxonomyTypeID,
            dbo.GenerateObjectUrl('Artifact', A.ArtifactTypeID, A.ID) as Url
    from	Artifact A 
            left join TaxonomyType T on T.ID = A.TaxonomyTypeID {1} 
            left join Artifact P on P.ID = A.ParentID 
    where    A.ArtifactTypeID = @id and A.[Visible] = 1";
                var model = processDynamicResults(
                    sql, Request, 
                    "ArtifactType", id, 
                    true, 
                    sortDataField, sortOrder, pagenum, pagesize, 
                    new string[] { "A.Name", "A.Status", "T.Name", "P.TextPath" }, 
                    filter, ownerUsers, ownerGroups, applyHiddenFilters: true, includeIdColumn: false);
                return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        private string addOwnershipJoinCriteria(string joins, string ownerUsers, string ownerGroups)
        {
            int index = 0;
            if (!string.IsNullOrEmpty(ownerUsers))
            {
                foreach (var user in ownerUsers.Split(','))
                {
                    var ids = user.Split('|');
                    if (ids.Length == 2)
                    {
                        joins += $" inner join responsibilitydetail RD{index} on (RD{index}.ObjectID = A.ID and RD{index}.Visible = 1 and RD{index}.ObjectType = 'Artifact' and RD{index}.ResponsibleObjectType = 'resource' and RD{index}.ResponsibleObjectID = {int.Parse(ids[1])} and RD{index}.ResponsibilityTypeID = {int.Parse(ids[0])} )";
                        index++;
                    }
                }
            }

            if (!string.IsNullOrEmpty(ownerGroups))
            {
                foreach (var group in ownerGroups.Split(','))
                {
                    var ids = group.Split('|');
                    if (ids.Length == 2)
                    {
                        joins += $" inner join responsibilitydetail RD{index} on (RD{index}.ObjectID = A.ID and RD{index}.Visible = 1 and RD{index}.ObjectType = 'Artifact' and RD{index}.ResponsibleObjectType = 'group' and RD{index}.ResponsibleObjectID = {int.Parse(ids[1])} and RD{index}.ResponsibilityTypeID = {int.Parse(ids[0])})";
                        index++;
                    }
                }                
            }

            return joins;
        }

        [Route("types")]
        public JsonNetResult GetTypes()
        {
            return new JsonNetResult
            {
                Data = Company.Table<ArtifactType>().OrderBy(i => i.Parent.Name).ThenBy(i => i.Name).Select(i => new { i.ID, i.Name, i.ParentID, expanded = true }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("typeswithstatistics")]
        public JsonNetResult GetTypesWithStatistics()
        {
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.ArtifactTypeStatisticsList),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion
    }
}