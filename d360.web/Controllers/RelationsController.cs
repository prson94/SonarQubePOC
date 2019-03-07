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

        public RelationsController(CommunityContext community, CompanyContext company)
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

        private FileResult IntersectTypeItemsExcelWithCustomColumns(int id,IEnumerable<string> customColumns )
        {
            
           var customColumnName = "[" + customColumns.Aggregate((x, y) => x + "],[" + y) + "]";
           var CteColumnName = "CTE.[" + customColumns.Aggregate((x, y) => x + "],CTE.[" + y) + "]";
         

            var sql = @"WITH CTE (ObjectID, " + customColumnName +
                ") AS ( SELECT ObjectId, " + customColumnName +
                " FROM ( select f2.ObjectID, f.FriendlyName,FormattedValue from fieldtype f  " +
                "inner join field f2 on f2.fieldtypeid = f.id where f.[object] = 'IntersectType'" +
                " and f.objectid = @id  ) as PivotData " +
                "PIVOT (max(FormattedValue) FOR FriendlyName IN (" + customColumnName + ") ) AS PivotResult) " +
                "select i.ID, i.[Subject],i.SubjectID, i.SubjectName, i.SubjectTypeName, i.[Object], " +
                "i.ObjectID, i.ObjectName, i.ObjectTypeName, i.PredicateName , " + CteColumnName +
                " from  intersectdetail as i left join CTE  on CTE.ObjectID =i.id where intersecttypeid=@id ";
            var models = Company.Query<dynamic>(sql, new { id = id });


            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Intersect ID");
            document.SetCellValue(1, index++, "Subject Type");
            document.SetCellValue(1, index++, "Subject ID");
            document.SetCellValue(1, index++, "Subject Name");
            document.SetCellValue(1, index++, "Subject Type Name");
            document.SetCellValue(1, index++, "Predicate");
            document.SetCellValue(1, index++, "Object Type");
            document.SetCellValue(1, index++, "Object ID");
            document.SetCellValue(1, index++, "Object Name");
            document.SetCellValue(1, index++, "Object Type Name");
            foreach (var col in customColumns)
            {
                document.SetCellValue(1, index++, col);
            }

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, (int)row.ID);
                document.SetCellValue(rowNumber, index++, (string)row.Subject);
                document.SetCellValue(rowNumber, index++, (int)row.SubjectID);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectName);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectTypeName);
                document.SetCellValue(rowNumber, index++, (string)row.PredicateName);
                document.SetCellValue(rowNumber, index++, (string)row.Object);
                document.SetCellValue(rowNumber, index++, (int)row.ObjectID);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectName);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectTypeName);
                foreach (var col in customColumns)
                {
                    var data = (IDictionary<string, object>)row;
                    document.SetCellValue(rowNumber, index++, (string)data[col]);
                }
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", string.Format("Relationship Type Items {0}.xlsx", System.DateTime.Now.ToShortDateString()));
        }
        [Route("_intersectTypeItems/{id:int}/excel.xls"), FileDownload, HttpGet]
        public FileResult _IntersectTypeItemsExcel(int id)
        {
            var customColumns = Company.Query<string>(
                @"select distinct  f.FriendlyName   as Name from fieldtype f  
				inner join field f2 on f2.fieldtypeid = f.id 
				 where f.[object] = 'IntersectType' and f.objectid = @id ", new { id = id });

            if (customColumns.Count() > 0) return IntersectTypeItemsExcelWithCustomColumns(id, customColumns);


            var models = Company.Query<dynamic>(
                @"select 
                    ID,
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
            document.SetCellValue(1, index++, "Intersect ID");
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
                document.SetCellValue(rowNumber, index++, (int)row.ID);
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
        public async Task<FileResult> _IntersectTypesExcel()
        {
            var queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("state", "1"));
            var models = await Company.GetIntersectTypes(queryParams);
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
