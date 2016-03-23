using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using d360.model;
using d360.web.Controllers;
using d360.core.entities;
using d360.web.Models;
using d360.core;
using d360.core.exceptions;
using System.Net;
using System.Xml.Linq;
using System.Globalization;

namespace d360.web.Controllers
{
    [RoutePrefix("monitor"), Authorize]
    public class MonitorController : BaseController
    {
        #region DI

        public MonitorController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        #region Json

        public JsonNetResult EventHeaders(int ruleID, string sortDataField, string sortOrder, int pagenum = 0, int pagesize = 20)
        {
            var querySql = @"select	A.ID,
		A.Name,
        T.Name as [Rule],
        A.PublicID,
        EC.[NumberOfEvents],
        ED.[Date]
from	EventGroup A 
        outer apply (
                    select count(1) as [NumberOfEvents] from Event where EventGroupID = A.ID
                    ) EC
        outer apply (
                    select max(Date) as Date from Event where EventGroupID = A.ID
                    ) ED
inner join [Rule] T on T.ID = A.RuleID and A.RuleID = @id";

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql); ;

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("id", ruleID);

            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            int total = Company.Query<int>(countSql, dbArgs).First();

            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder);
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult EventsByHeader(int groupID, string sortDataField, string sortOrder, int pagenum = 0, int pagesize = 20)
        {
            var joins = "";
            var columns = "";

            var eventGroup = Company.GetById<EventGroup>(groupID);

            if (eventGroup != null)
            {
                getDynamicFieldJoinStatements(eventGroup.RuleID ?? 0, "Rule", out joins, out columns);

                var querySql = string.Format(@"select A.ID,
		G.Name,
        T.Name as [Rule],
		A.Date,
        case A.Criticality
            when 5 then 'Critical'
            when 4 then 'High'
            when 3 then 'Medium'
            when 2 then 'Low'
            else 'Negligible'
        end as Criticality,
        A.Status,
        {0}
        A.SourceID
from	Event A 
inner join EventGroup G on G.ID = A.EventGroupID
inner join [Rule] T on T.ID = G.RuleID and A.EventGroupID = @id {1}", columns, joins);

                var countSql = string.Format(@"select count(1) from ({0}) A", querySql);

                var sql = string.Format(@"select * from ({0}) A", querySql);

                var dbArgs = new Dapper.DynamicParameters();
                dbArgs.Add("id", groupID);

                countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
                int total = Company.Query<int>(countSql, dbArgs).First();

                sql = applyFilteringSuffixBind(sql, Request, dbArgs);
                sql = applySortSuffix(sql, sortDataField, sortOrder);
                sql = applyPagingSuffix(sql, pagenum, pagesize);

                var query = Company.Query<dynamic>(sql, dbArgs);

                return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
            }
            else 
            {
                return new JsonNetResult { Data = new { message = "Event Group not found." } };
            }
        }

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

        #endregion
    }
}
