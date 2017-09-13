using System;
using System.Linq;
using System.Web.Mvc;
using d360.model;
using d360.core.entities;
using d360.web.Models.Attributes;
using SpreadsheetLight;
using System.IO;
using Newtonsoft.Json;
using d360.web.Models;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/monitor"), Authorize]
    public class MonitorController : BaseController
    {
        #region DI

        public MonitorController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        #region Json

        [Route("policystatusfordate"), NonNullableParameters]
        public JsonNetResult PolicyStatusForDate(int id, DateTime date)
        {
            var sql = @"
			with PH as	(
						select	ID,
								ParentID
						from	Policy
						where	ID = @id
						union all
						select	C.ID,
								C.ParentID
						from	Policy C 
								inner join PH on C.ParentID = PH.ID
						)

			select	case 
				when	exists(
							SELECT	1
							FROM	[Event] E
									INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
									INNER JOIN [Rule] R on R.ID = G.RuleID
							where	R.ID in (
											select	distinct
													CR.TargetObjectID
											from	PH
													inner join cache.Relationships CR on CR.SourceObject = 'Policy' and CR.SourceObjectID = PH.ID and CR.TargetObject = 'Rule'
											)
									and E.Date between @minDate and @maxDate 
									and E.Status <> 'Closed'
						) then cast(0 as bit)
				else cast(1 as bit)
			end as Status";
            date = date.Date;
            var minDate = date;
            var maxDate = date.AddDays(1);
            var status = Company.Query<bool>(sql, new { id, minDate, maxDate }).SingleOrDefault();

            return new JsonNetResult { Data = new { status }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("rules/{id:int}/results")]
        public JsonNetResult GetRuleResults(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter)
        {
            var dbArgs = new Dapper.DynamicParameters();

            dbArgs.Add("id", id);

            var querySql = @"select	A.*
{1}
from	RuleResult A 
        {0}
where   A.RuleImplementationID = @id";

            var ruleQualifiers = Company.Query<RuleQualifierTypeField>(@"select Name as Header, replace(Name, ' ', '') as Field from RuleResultQualifierType where RuleImplementationID = @id order by [Order]", new { id }).ToList();
            var qualifierFieldsSql = "";

            if (ruleQualifiers.Count > 0)
            {
                qualifierFieldsSql = @"
                        left join
		                        (select * from
			                        (select q.RuleResultID as ResID, replace(QT.[Name], ' ', '') as N, Q.[Value] as Val from RuleResultQualifierType QT
			                        join RuleResultQualifier Q on Q.RuleResultQualifierTypeID = QT.ID
			                        where QT.RuleImplementationID = @id) as vt
			                        pivot
			                        (
			                        max(Val) for N in (
			                        {0}
			                        )
			                        ) as qr) as RQ on RQ.ResID = A.ID
                                    ";
                qualifierFieldsSql = string.Format(qualifierFieldsSql, string.Join(",", ruleQualifiers.Select(q => q.Field)));
            }

            querySql = string.Format(querySql, qualifierFieldsSql, (ruleQualifiers.Count > 0) ? ",RQ.*" : "");

            //if simple filter specified add that citeria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                querySql = $@"{querySql} and {addDynamicFieldSimpleFilter(new string[] { 
                    "A.EffectiveDate", 
                    "A.RowsPassed",
                    "A.RowsFailed",
                    "A.PassFraction",
                    "A.FailFraction",
                    "A.Passed",
                    "F.TextPath"
                }, "RuleResult", id, filter, dbArgs)}";
            }

            querySql = applyRelationFilteringExists(querySql, Request, dbArgs);

           

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);


            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs, true);
            sql = applyFilteringSuffixBind(sql, Request, dbArgs, true);


            sql = applySortSuffix(sql, sortDataField, sortOrder, "EffectiveDate", "desc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            int total = Company.Query<int>(countSql, dbArgs).First();
            var query = Company.Query<dynamic>(sql, dbArgs);

            return new JsonNetResult { Data = new { total, results = query, qualifiers = ruleQualifiers }, Formatting = Formatting.None };
        }

        #endregion

        [Route("ExportResultsByRule"), FileDownload, NonNullableParameters]
        public FileResult ExportQueryItemsByAttributeType(int id)
        {
            var detail = Company.GetObjectDetail("Rule", id);
            var results = Company.Filter<RuleResult>(i => i.RuleImplementation.RuleID == id).OrderByDescending(i => i.EffectiveDate);

            #region Create the list sheet

            var document = new SLDocument();
            var defaultSheet = detail.PluralizedName;
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, defaultSheet);
            document.SelectWorksheet(defaultSheet);

            var row = 1;

            #region Header

            document.SetCellValue(row, 1, "Effective Date");
            document.SetCellValue(row, 2, "Rows Passed");
            document.SetCellValue(row, 3, "Rows Failed");
            document.SetCellValue(row, 4, "Passed");
            document.SetCellValue(row, 5, "Created On");

            #endregion

            foreach (var item in results)
            {
                row++;
                document.SetCellValue(row, 1, item.EffectiveDate.ToShortDateString());
                document.SetCellValue(row, 2, item.RowsPassed);
                document.SetCellValue(row, 3, item.RowsFailed);
                document.SetCellValue(row, 4, (item.Passed) ? "Y" : "N");
                document.SetCellValue(row, 5, item.CreatedOn.ToShortDateString());
            }

            document.AutoFitColumn(1, 5);

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"{detail.PluralizedName}.xlsx");
        }

    }
}
