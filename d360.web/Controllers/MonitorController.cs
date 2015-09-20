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

            countSql = applyFilteringSuffix(countSql, Request);
            int total = Company.Query<int>(countSql, new { id = ruleID }).First();

            sql = applyFilteringSuffix(sql, Request);
            sql = applySortSuffix(sql, sortDataField, sortOrder);
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, new { id = ruleID });

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

                countSql = applyFilteringSuffix(countSql, Request);
                int total = Company.Query<int>(countSql, new { id = groupID }).First();

                sql = applyFilteringSuffix(sql, Request);
                sql = applySortSuffix(sql, sortDataField, sortOrder);
                sql = applyPagingSuffix(sql, pagenum, pagesize);

                var query = Company.Query<dynamic>(sql, new { id = groupID });

                return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
            }
            else 
            {
                return new JsonNetResult { Data = new { message = "Event Group not found." } };
            }
        }

        #region Policy/Rule Tree Hierarchy

        class PolicyRuleHierarchyItem
        {
            public PolicyRuleHierarchyItem()
            {
                expanded = true;
            }
            public string MergedID { get; set; }
            public string Type { get; set; }
            public int? ID { get; set; }
            public string Name { get; set; }
            public List<PolicyRuleHierarchyItem> Items { get; set; }
            public bool expanded { get; set; }
        }

        [Route("hierarchy")]
        public JsonResult PolicyRuleHierarchy()
        {
            var policies = Company.Table<Policy>().ToList();
            var rules = Company.Table<Rule>().ToList();

            var root = new PolicyRuleHierarchyItem { MergedID = "Policy|0", ID = null, Name = "Events", Type = SystemObjects.Policy.ToString() };
            root.Items = nestHierarchyNode(policies, rules, root);

            var list = new List<PolicyRuleHierarchyItem>() { root };

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        List<PolicyRuleHierarchyItem> nestHierarchyNode(List<Policy> policies, List<Rule> rules, PolicyRuleHierarchyItem parent)
        {
            var list = new List<PolicyRuleHierarchyItem>();

            foreach (var p in policies.Where(i => i.ParentID == parent.ID).OrderBy(i => i.Name))
            {
                var child = new PolicyRuleHierarchyItem { MergedID = "Policy|" + p.ID, ID = p.ID, Name = p.Name, Type = SystemObjects.Policy.ToString() };
                child.Items = nestHierarchyNode(policies, rules, child);
                list.Add(child);
            }
            foreach (var r in rules.Where(i => i.PolicyID == parent.ID).OrderBy(i => i.Name))
            {
                list.Add(
                    new PolicyRuleHierarchyItem { MergedID = "Rule|" + r.ID, ID = r.ID, Name = r.Name, Type = SystemObjects.Rule.ToString() }
                );
            }

            return list;
        }

        #endregion

        #endregion
    }
}
