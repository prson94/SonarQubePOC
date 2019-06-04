using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;
using Dapper;

namespace d360.model.DataAccessLayer
{
    public class WorkflowRepository : IWorkflowRepository
    {
        private ICompanyContext CompanyContext;
        public WorkflowRepository(ICompanyContext CompanyContext)
        {
            this.CompanyContext = CompanyContext;
        }
        public async Task<IEnumerable<WorkflowTypeApiViewModel>> GetWorkflowTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            string whereClause = string.Empty;
            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "actiontypeuid"))
                {
                    Guid actionTypeUid;
                    var actionTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "actiontypeuid").Value;
                    if ((Guid.TryParse(actionTypeUidString, out actionTypeUid)) && (actionTypeUid != Guid.Empty))
                    {
                        dbArgs.Add("@actionTypeUid", actionTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" [is_t].[UID] = @actionTypeUid";
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "assettypeuid"))
                {
                    Guid assetTypeUid;
                    var assettypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
                    if ((Guid.TryParse(assettypeUidString, out assetTypeUid)) && (assetTypeUid != Guid.Empty))
                    {
                        dbArgs.Add("@assetTypeUid", assetTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" [d].[UID] = @assetTypeUid";
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "relationshiptypeuid"))
                {
                    Guid relationshipTypeUid;
                    var relationshipTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
                    if ((Guid.TryParse(relationshipTypeUidString, out relationshipTypeUid)) && (relationshipTypeUid != Guid.Empty))
                    {
                        dbArgs.Add("@relationshipTypeUid", relationshipTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" [IT].[UID] = @relationshipTypeUid";
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "state"))
                {
                    State state;
                    var stateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "state").Value;
                    if (Enum.TryParse(stateString, out state))
                    {
                        dbArgs.Add("@state",(int) state);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" t.State = @state";
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "changetype"))
                {
                    ChangeType changeType;
                    var changeTypeString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "changetype").Value;
                    if (Enum.TryParse(changeTypeString, out changeType))
                    {
                        dbArgs.Add("@changeType",(int) changeType);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" e.ChangeType = @changeType";
                    }
                }


            }


                string sql = $@"			select 
				case when e.[Object] = 'IssueType' then is_t.uid
					ELSE NULL END as ActionTypeUid ,
				case when e.[Object] = 'ArtifactType' or e.[Object] = 'RuleType' or e.[Object] = 'PolicyType'
					or e.[Object] = 'TaxonomyType' or e.[Object] = 'ShoppingCartType' or  e.[Object] = 'ReferenceItemType'
					or e.[Object] = 'Fusion' then d.uid
				ELSE NULL END as AssetTypeUid,
				case when e.[Object] = 'IntersectType' then IT.uid
					ELSE NULL END as RelationshipTypeUid,
				t.Name,
				t.Description,
                e.ChangeType,
				case when t.PublishedVersionID is not null then v.uid
					else null end as PublishedVersionUid,
					t.CreatedOn,
					t.UpdatedOn,
					t.State as State
				from workflow.type t
				inner join workflow.eventregistration e on e.typeid = t.id
				left join AssetType D on D.Object = E.Object and D.ObjectID = e.ObjectID 
				left join issuetype is_t on e.object = 'IssueType' and is_t.id = e.objectid
				left join IntersectType IT on e.Object = 'IntersectType' and e.objectid = IT.ID
				left join workflow.version v on v.id = t.publishedversionid
				{whereClause}
				order by t.Name asc";

            var workflowTypes = await this.CompanyContext.QueryAsync<WorkflowTypeApiViewModel>(sql, dbArgs);
            return workflowTypes;
        }

        public async Task<WorkflowVersionsApiViewModel> GetWorkflowVersions(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            List<string> whereClause = new List<string>();
            List<string> pagingSql = new List<string>();
            WorkflowVersionsApiViewModel model = new WorkflowVersionsApiViewModel();

            this.gettWorkflowVersionsQueryParamsSql(model, dbArgs, whereClause, pagingSql, queryParams);

            var whereSql = "";
            if (whereClause.Any())
                whereSql = $"where {string.Join(" and ", whereClause)}";

            string countSql = $@"select 
                count(*)
				from workflow.type t
				inner join workflow.eventregistration e on e.typeid = t.id
				inner join workflow.version v on v.TypeID = t.Id
				left join AssetType D on D.Object = E.Object and D.ObjectID = e.ObjectID 
				left join issuetype is_t on e.object = 'IssueType' and is_t.id = e.objectid
				left join IntersectType IT on e.Object = 'IntersectType' and e.objectid = IT.ID
				left outer join reporting.Global_Resource R on R.ResourceID = t.CreatedBy
				left outer join reporting.Global_Resource R1 on R1.ResourceID = t.UpdatedBy
				{whereSql}";

            string sql = $@"			select 
                V.UID as Uid,
				case when e.[Object] = 'IssueType' then is_t.uid
					ELSE NULL END as ActionTypeUid ,
				case when e.[Object] = 'ArtifactType' or e.[Object] = 'RuleType' or e.[Object] = 'PolicyType'
					or e.[Object] = 'TaxonomyType' or e.[Object] = 'ShoppingCartType' or  e.[Object] = 'ReferenceItemType'
					or e.[Object] = 'Fusion' then d.uid
				ELSE NULL END as AssetTypeUid,
				case when e.[Object] = 'IntersectType' then IT.uid
					ELSE NULL END as RelationshipTypeUid,
				t.Uid as WorkflowTypeUid,
				case when t.PublishedVersionID = V.Id then 0
					else 1 end as IsPublished,
                    v.[version] as VersionNumber,
					t.CreatedOn,
					t.UpdatedOn,
					t.State as State,
					R.uid as CreatedByUid,
					R1.uid as UpdatedByUid,
                    TotalWorkflowItems = (select count(*) from workflow.Item where versionId=V.id),
                    TotalPendingWorkflowItems = (select count(*) from workflow.Item where versionId=V.id and CompletedOn is null)
				from workflow.type t
				inner join workflow.eventregistration e on e.typeid = t.id
				inner join workflow.version v on v.TypeID = t.Id
				left join AssetType D on D.Object = E.Object and D.ObjectID = e.ObjectID 
				left join issuetype is_t on e.object = 'IssueType' and is_t.id = e.objectid
				left join IntersectType IT on e.Object = 'IntersectType' and e.objectid = IT.ID
				left outer join reporting.Global_Resource R on R.ResourceID = t.CreatedBy
				left outer join reporting.Global_Resource R1 on R1.ResourceID = t.UpdatedBy
				{whereSql}
                {string.Join("\n", pagingSql)}";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs);
            var count = countResults.First();

            var results = await CompanyContext.QueryAsync<WorkflowVersionApiViewModel>(sql, dbArgs);

            model.items = results;
            model.total = count;

            return model;
        }

        private void gettWorkflowVersionsQueryParamsSql(WorkflowVersionsApiViewModel model, DynamicParameters dbArgs, List<string> whereClause, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            if (queryParams != null)
            {

                var orderBySql = "";
                var offsetSql = "";
                var pageNum = -1;
                var pageSize = -1;

                if (!queryParams.Any(p => p.Key == "_order"))
                {
                    orderBySql = "order by v.[version] asc";
                }

                queryParams.ToList().ForEach(x => {

                    switch (x.Key.ToLower())
                    {
                        case "actiontypeuid":
                            Guid actionTypeUid;
                     
                            if ((Guid.TryParse(x.Value, out actionTypeUid)) && (actionTypeUid != Guid.Empty))
                            {
                                dbArgs.Add("@actionTypeUid", actionTypeUid);
                                whereClause.Add(" [is_t].[UID] = @actionTypeUid");
                            }
                            break;
                        case "assettypeuid":
                            Guid assetTypeUid;
                            if ((Guid.TryParse(x.Value, out assetTypeUid)) && (assetTypeUid != Guid.Empty))
                            {
                                dbArgs.Add("@assetTypeUid", assetTypeUid);
                                whereClause.Add("[d].[UID] = @assetTypeUid");
                            }
                            break;
                        case "relationshiptypeuid":
                            Guid relationshipTypeUid;
                            if ((Guid.TryParse(x.Value, out relationshipTypeUid)) && (relationshipTypeUid != Guid.Empty))
                            {
                                dbArgs.Add("@relationshipTypeUid", relationshipTypeUid);
                                whereClause.Add(" [IT].[UID] = @relationshipTypeUid");
                            }
                            break;
                        case "workflowtypeuid":
                            Guid workflowTypeUid;
                            if ((Guid.TryParse(x.Value, out workflowTypeUid)) && (workflowTypeUid != Guid.Empty))
                            {
                                dbArgs.Add("@workflowTypeUid", workflowTypeUid);
                                whereClause.Add(" [T].[UID] = @workflowTypeUid");
                            }
                            break;
                        case "state":
                            State state;
                            if (Enum.TryParse(x.Value, out state))
                            {
                                dbArgs.Add("@state", (int)state);
                                whereClause.Add("t.State = @state");
                            }
                            break;
                        case "_pagesize":
                            if (int.TryParse(x.Value, out pageSize))
                            {
                                if (pageSize < 1) pageSize = 1;
                            }
                            break;
                        case "_pageno":
                             if (int.TryParse(x.Value, out pageNum))
                            {
                                if (pageNum < 1) pageNum = 1;
                            }
                            break;
                        case "_order":
                            switch (x.Value)
                            {
                                case "VersionNumber":
                                    orderBySql = "order by v.[version] asc";
                                    break;
                                case "State":
                                    orderBySql = "order by t.State asc";
                                    break;
                                case "CreatedOn":
                                    orderBySql = "order by t.CreatedOn asc";
                                    break;
                                case "UpdatedOn":
                                    orderBySql = "order by t.UpdatedOn asc";
                                    break;
                            }
                            break;

                    }

                   
                });

                pagingSql.Add(orderBySql);

                if (pageSize > 0 || pageNum > 0)
                {
                    if (pageSize < 1) pageSize = 1;
                    if (pageNum < 1) pageNum = 1;

                    model.pageSize = pageSize;
                    model.pageNum = pageNum;

                    offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                    pagingSql.Add(offsetSql);
                }
            }
        }



        public async Task<IEnumerable<WorkflowVersionSteps>> GetWorkflowVersionSteps(Guid uid)
        {
            var dbArgs = new DynamicParameters();
            string whereClause = " where v.uid=@uid";
            dbArgs.Add("@uid", uid);
            string sql = $@"	select 
		            itemstep.UID,
		            vs.Name,
		            VS.State,
		            vs.StepType,
		            vs.ActivityType,
		            itemstep.Settings,
		            itemstep.StartedOn,
		            itemstep.CompletedOn,
		            R.uid as StartedByUid,
		            R1.uid as CompletedByUid
	            from workflow.[version]  v 
	            inner join  workflow.VersionStep  vs  on
	            vs.VersionID = v.id
	            inner join workflow.ItemStep itemstep on
	            itemstep.StepID = vs.id
	            left outer join reporting.Global_Resource R on R.ResourceID = itemstep.StartedBy
	            left outer join reporting.Global_Resource R1 on R1.ResourceID = itemstep.CompletedBy
	            {whereClause}"; 

            var workflowVersionSteps = await this.CompanyContext.QueryAsync<WorkflowVersionSteps>(sql, dbArgs);
            return workflowVersionSteps;
        }
    }
}
