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
using System.Threading.Tasks;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("relations"), Authorize]
    public class RelationsController : BaseController
    {
        #region DI

        public RelationsController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        { }

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
                        p.ID,
                        p.Name,
                        p.Inverse,
                        p.IsSystem,
                        IsUsed = usage.Any(i => i == p.ID),
                        Type = p.Type.GetDisplayName(),
                        p.UID
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

        [Route("_IntersectTypes/excel.xls"), FileDownload, HttpGet]        
        public async Task<FileResult> _IntersectTypesExcel()
        {
            var queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("state", "1"));
            var models = await Company.GetRelationshipTypes(queryParams);
            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Id");
            document.SetCellValue(1, index++, "Uid");
            document.SetCellValue(1, index++, "Subject");
            document.SetCellValue(1, index++, "Subject Class");
            document.SetCellValue(1, index++, "Predicate");            
            document.SetCellValue(1, index++, "Object");
            document.SetCellValue(1, index++, "Object Class");

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, row.Id);
                document.SetCellValue(rowNumber, index++, row.Uid.ToString());
                document.SetCellValue(rowNumber, index++, row.Subject.Name);
                document.SetCellValue(rowNumber, index++, row.Subject.Class.ToString());
                document.SetCellValue(rowNumber, index++, row.Predicate.Name);
                document.SetCellValue(rowNumber, index++, row.Object.Name);
                document.SetCellValue(rowNumber, index++, row.Object.Class.ToString());                
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", string.Format("Relationship Types {0}.xlsx", System.DateTime.Now.ToShortDateString()));
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


        #endregion
    }
}
