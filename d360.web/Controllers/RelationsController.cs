using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("relations"), Authorize]
    public class RelationsController : BaseController
    {
        #region DI

        public RelationsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Models

        public class SourcesToObjectModel
        {
            public int ID { get; set; }
            public int IntersectID { get; set; }
            public int IntersectTypeID { get; set; }
            public string Type { get; set; }
            public bool IsStart { get; set; }
            public bool IsEnd { get; set; }
            public int Level { get; set; }
            public int NodeID { get; set; }
            public string TypeName { get; set; }
            public string ObjectType { get; set; }
            public int ObjectTypeID { get; set; }
            public string ObjectName { get; set; }
            public string O { get; set; }
            public int OID { get; set; }
            public string BackColor { get; set; }
            public string ForeColor { get; set; }
            public int PredicateID { get; set; }
            public string Predicate { get; set; }
            public int RawSourceRuleCount { get; set; }
            public int SourceRuleCount { get; set; } = 0;
            public int RawMappingRuleCount { get; set; }
            public int LinkMappingRuleCount { get; set; }
            public int ChallengeCount { get; set; }
            public int OpenEventCount { get; set; }
            public int OpenIssueCount { get; set; }
            public int RawTransformationCount { get; set; }
            public int LinkTransformationCount { get; set; }
        }

        /// <summary>
        /// This is the new model that corresponds to GetHierarchyByPredicateType stored procedure.
        /// </summary>
        public class HierarchyViewModel
        {
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string ObjectType { get; set; }
            public int ObjectTypeID { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string ObjectTypeName { get; set; }
            public int Level { get; set; }
            public int GroupNumber { get; set; }
        }

        public class HierarchyModel
        {
            public int ID { get; set; }
            public string Subject { get; set; }
            public string Object { get; set; }
            public int SubjectID { get; set; }
            public int ObjectID { get; set; }
            public string ObjectType { get; set; }
            public int ObjectTypeID { get; set; }
            public string ParentID { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public string Url { get; set; }
            public string ObjectTypeName { get; set; }
            public int Level { get; set; }
            public int PredicateID { get; set; }
            public string PredicatePhrase { get; set; }
            public PredicateType Type { get; set; }
            public int GroupNumber { get; set; }
            public string UID { get; set; }

        }

        public class HierarchyArtifactsModel
        {
            public PredicateType MapType { get; set; }
            public SystemObjects Type { get; set; }
            public int ID { get; set; }
            public bool IsSubject { get; set; }
        }

        #endregion

        #region Json

        [HttpGet, Route("Predicates")]
        public JsonNetResult Predicates()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            var predicates = Company.Table<Predicate>().OrderBy(i => i.Name);
            var usage = Company.Filter<IntersectType>(i => i.PredicateID.HasValue).Select(i => i.PredicateID.Value).Distinct().ToList();
            var data = new List<dynamic>();

            predicates.ToList().ForEach(p =>
                {
                    data.Add(new
                    {
                        ID = p.ID,
                        Name = p.Name,
                        Inverse = p.Inverse,
                        p.IsSystem,
                        IsUsed = usage.Any(i => i == p.ID),
                        Type = p.Type.GetDisplayName()
                    });
            });

            return new JsonNetResult
            {
                Data = data,
                Formatting = Formatting.None
            };
        }

        [HttpGet, Route("GetPredicates")]
        public JsonNetResult GetPredicates()
        {
            var list = Company.Query<dynamic>(@"select ID as [value], Name as [text] from Predicate order by Name");
            return new JsonNetResult { Data = list, Formatting = Formatting.None };
        }

        [Route("_IntersectTypes")]
        public JsonNetResult _IntersectTypes()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            var models = Company.Query<dynamic>(
@"select    ID,
			Subject,
			SubjectID,
			SubjectName,
            PredicateID,
            PredicateName,
			Object,
			ObjectID,
			ObjectName
from		IntersectTypeDetail
where       IsSystem = 0
order by	SubjectName,
			ObjectName");
            return new JsonNetResult { Data = models, Formatting = Formatting.None };
        }

        [Route("_intersectTypeItems/{id:int}/excel.xls"), FileDownload, HttpGet]
        public FileResult _IntersectTypeItemsExcel(int id)
        {
            var models = Company.Query<dynamic>(
                @"select 
                    [Subject], 
                    SubjectID, 
                    SubjectName, 
                    SubjectTypeName, 
                    [Object], 
                    ObjectID, 
                    ObjectName, 
                    ObjectTypeName, 
                    PredicateName 
                from 
                    intersectdetail 
                where intersecttypeid = @id", new { id = id });

            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Subject Type");
            document.SetCellValue(1, index++, "Subject ID");
            document.SetCellValue(1, index++, "Subject Name");
            document.SetCellValue(1, index++, "Subject Type Name");
            document.SetCellValue(1, index++, "Predicate");
            document.SetCellValue(1, index++, "Object Type");
            document.SetCellValue(1, index++, "Object ID");
            document.SetCellValue(1, index++, "Object Name");
            document.SetCellValue(1, index++, "Object Type Name");
            
            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, (string)row.Subject);
                document.SetCellValue(rowNumber, index++, (int)row.SubjectID);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectName);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectTypeName);
                document.SetCellValue(rowNumber, index++, (string)row.PredicateName);
                document.SetCellValue(rowNumber, index++, (string)row.Object);
                document.SetCellValue(rowNumber, index++, (int)row.ObjectID);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectName);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectTypeName);
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", string.Format("Relationship Type Items {0}.xlsx", System.DateTime.Now.ToShortDateString()));
        }

        [Route("_IntersectTypes/excel.xls"), FileDownload, HttpGet]        
        public FileResult _IntersectTypesExcel()
        {
            var models = Company.Query<dynamic>(
@"select    ID,
			Subject,
			SubjectID,
			SubjectName,
            PredicateID,
            PredicateName,
			Object,
			ObjectID,
			ObjectName
from		IntersectTypeDetail
where       IsSystem = 0
order by	SubjectName,
			ObjectName");

            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "ID");
            document.SetCellValue(1, index++, "Subject");
            document.SetCellValue(1, index++, "Subject Type");
            document.SetCellValue(1, index++, "Predicate");            
            document.SetCellValue(1, index++, "Object");
            document.SetCellValue(1, index++, "Object Type");

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, (int)row.ID);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectName);
                document.SetCellValue(rowNumber, index++, (string)row.Subject);
                document.SetCellValue(rowNumber, index++, (string)row.PredicateName);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectName);
                document.SetCellValue(rowNumber, index++, (string)row.Object);                
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", string.Format("Relationship Types {0}.xlsx", System.DateTime.Now.ToShortDateString()));
        }

        [HttpGet, Route("PossibleRelationshipsByIntersect"), NonNullableParameters]
        public JsonNetResult PossibleRelationshipsByIntersect(int id)
        {
            var list = Company.Query<AllowedIntersectionType>("GetAllowedIntersectionTypesByIntersect @intersectID", new { intersectID = id }).ToList().Select(i => new ContextToolbarItem {
                Icon = "plus",
                Title = i.TargetName,
                Type = "local",
                Uri = "/form/AddRelationship?intersectTypeID=" + i.IntersectTypeID + "&type=Intersect&id=" + i.ParentIntersectID                
            });
            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("GetPossibleRelationshipsObjectByIntersect"), NonNullableParameters]
        public JsonNetResult GetPossibleRelationshipsObjectByIntersect(int id)
        {
            var list = Company.Query<AllowedIntersectionType>("GetAllowedIntersectionTypesByIntersect @intersectID", new { intersectID = id }).ToList().Select(i => new 
            {                
                Title = i.TargetName,                
                IntersectTypeID = i.IntersectTypeID,
                ParentIntersectID = i.ParentIntersectID,
                ObjectType = i.TargetType
            });
            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #region Hierarchy

        [HttpGet, Route("hierarchy/{mapType}/{type}/{id:int}")]
        public JsonNetResult GetHierarchy(SystemObjects type, int id, PredicateType mapType)
        {
            return new JsonNetResult
            {
                Data = new { },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, Route("hierarchy/artifacts")]
        public JsonNetResult GetHierarchyArtifactsNg(HierarchyArtifactsModel model)
        {
            return GetHierarchyArtifacts(model);
        }

        [HttpGet, Route("hierarchy/artifacts")]
        public JsonNetResult GetHierarchyArtifacts(HierarchyArtifactsModel model)
        {
            return new JsonNetResult
            {
                Data = null,//itemList,
                Formatting = Formatting.None
            };
        }

        #endregion Hierarchy

        [HttpGet, Route("ChildRelationshipsBySourceAndTarget"), NonNullableParameters]
        public JsonNetResult ChildRelationshipsBySourceAndTarget(SystemObjects s, int sID, SystemObjects t, int tID)
        {
            var sType = s.ToString();
            var tType = t.ToString();
            var sql = $@"
select T.Object,
		T.ObjectID,
		T.ObjectUrl,
		T.ObjectName,
		T.ObjectTypeName
from[Intersect] O
    inner join[IntersectDetail] T on (
                                       ( (O.Subject = @s and O.SubjectID = @sid) AND (O.Object = @o and O.ObjectID = @oid) ) OR
                                       ( (O.Subject = @o and O.SubjectID = @oid) AND (O.Object = @s and O.ObjectID = @sid) )
							        )
									and T.Subject = 'Intersect' and T.SubjectID = O.ID";

            return new JsonNetResult { Data = Company.Query<dynamic>(sql, new { s = new Dapper.DbString { Value = sType, IsAnsi = true, IsFixedLength = true, Length = 50 }, sid = sID, o = new Dapper.DbString { Value = tType.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, oid = tID }).OrderBy(i => i.ObjectTypeName).ThenBy(i => i.ObjectName), Formatting = Formatting.None };
        }

        JArray convertList(JToken i)
        {
            if (i == null)
            {
                return null;
            }
            else
            {
                if (i is JArray)
                {
                    return (JArray)i;
                }
                else
                {
                    return new JArray(i);
                }
            }
        }

        #endregion
    }
}
