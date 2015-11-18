using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.web.Models;
using d360.model;
using d360.core;
using System.IO;
using d360.web.Models.Attributes;
using System.Diagnostics;
using SpreadsheetLight;
using System.Data;

namespace d360.web.Controllers
{
    [RoutePrefix("artifacts"), Authorize]
    public class ArtifactsController : BaseController
    {
        #region DI

        public ArtifactsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Exports

        [Route("{id:int}.xls"), FileDownload, HttpPost]
        public FileResult ToExcel(int id, ArtifactListFilterModel model)//string Name)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Artifact", out joins, out columns);

            var sql = string.Format(@"select * from (
select	A.ID,
		A.Name,
		A.Description,
		A.TextPath,
		A.Status,
		V.Name as SubjectArea,
        {0}
		dbo.GenerateObjectUrl('Artifact', A.ArtifactTypeID, A.ID) as Url
from	Artifact A inner join TaxonomyType V on V.ID = A.TaxonomyTypeID and A.ArtifactTypeID = {2} {1}) A", columns, joins, id);

            var type = Company.GetById<ArtifactType>(id);

            var document = new SLDocument();
            document.AddWorksheet("Items");

            // The data reader.
            var query = Company.Read(sql);
            var metafields = query.GetSchemaTable();

            #region Create the list sheet

            #region Header

            for (int i = 0; i < metafields.Rows.Count; i++)
            {
                document.SetCellValue(1, i, (string)metafields.Rows[i]["ColumnName"]);
            }

            #endregion

            int r = 1;
            while (query.Read())
            {
                r++;
                for (int i = 0; i < metafields.Rows.Count; i++)
                {
                    document.SetCellValue(r, i, query[i].ToString());
                }
            }

            metafields = null;
            query.Dispose();

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", string.Format("Filtered {0} List for {1}.xls", type.Name, DateTime.Now.ToShortDateString()));
        }

        #endregion

        #region Json

        [HttpPost]
        public JsonNetResult ByParent(int parentID, string sortDataField, string sortOrder, int pagenum = 0, int pagesize = 20, int childArtifactTypeID = 0)
        {
            Trace.TraceInformation("Calling ArtifactsController.ByParent : {0}, {1}", parentID, childArtifactTypeID);

            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(childArtifactTypeID, "Artifact", out joins, out columns);

            var querySql = string.Format(@"select	A.ID,
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
        where A.ArtifactTypeID = @id and A.ParentID = @p", columns, joins);

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            countSql = applyFilteringSuffix(countSql, Request);
            int total = Company.Query<int>(countSql, new { id = childArtifactTypeID, p = parentID }).First();

            sql = applyFilteringSuffix(sql, Request);
            sql = applySortSuffix(sql, sortDataField, sortOrder);
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, new { id = childArtifactTypeID, p = parentID });

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };        
        }

        [HttpPost]
        public JsonNetResult ByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize)
        {
            Trace.TraceInformation("Calling ArtifactsController.ByType : {0}", id);

            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Artifact", out joins, out columns);

            var querySql = string.Format(@"select	A.ID,
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
where    A.ArtifactTypeID = @id", columns, joins);

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);

            //var relationFilterSql = "";

            //var RelationshipIncludeType = Request["RelationshipIncludeType"];
            //var RelationshipObjectType = Request["RelationshipObjectType"];
            //var RelationshipObjectIDs = Server.UrlDecode(Request["RelationshipObjectIDs"]);

            //if (!string.IsNullOrEmpty(RelationshipObjectIDs))
            //{
            //    var IDs = RelationshipObjectIDs.Split(',').ToList();
            //    if (RelationshipIncludeType == "All")
            //    {
            //        IDs.ForEach(ID =>
            //        {
                        
            //            relationFilterSql = (string.IsNullOrEmpty(relationFilterSql) ? "" : " AND ") + 
            //                "A.ID in (select SourceObjectID from cache.Relationships where SourceObject = 'Artifact' and TargetObject = '" + RelationshipObjectType + "' and TargetObjectID = " + ID + ")";
            //        });
            //    }
            //    else
            //    {
            //        var idList = "";
            //        IDs.ForEach(ID =>
            //        {
            //            idList += (string.IsNullOrEmpty(idList) ? "" : ", ") + ID;
            //        });
            //        relationFilterSql = "A.ID in (select SourceObjectID from cache.Relationships where SourceObject = 'Artifact' and TargetObject = '" + RelationshipObjectType + "' and TargetObjectID in (" + idList + "))";
            //    }
            //}

            countSql = applyFilteringSuffix(countSql, Request);
            sql = applyFilteringSuffix(sql, Request);

            //if (!string.IsNullOrEmpty(relationFilterSql))
            //{
            //    countSql += ((countSql.ToLower().Contains("where")) ? " AND " : " WHERE ") + relationFilterSql;
            //    sql += ((sql.ToLower().Contains("where")) ? " AND " : " WHERE ") + relationFilterSql;
            //}

            sql = applySortSuffix(sql, sortDataField, sortOrder);
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            int total = Company.Query<int>(countSql, new { id = id }).First();
            var query = Company.Query<dynamic>(sql, new { id = id });

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
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

         #endregion
    }
}