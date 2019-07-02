using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using d360.core.entities;
using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                var pageSize = model.pageSize != 0 ? model.pageSize : -1;

              

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
                        case "_pagenum":
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


                if (string.IsNullOrEmpty(orderBySql))
                {
                    orderBySql = "order by v.[version] asc";
                }

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



        public async Task<IEnumerable<WorkflowVersionStepsApiViewModel>> GetWorkflowVersionSteps(Guid uid)
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
                    itemstep.Settings as SettingsXml,
                    itemstep.Fields as FieldsXml,
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

            var workflowVersionSteps = await this.CompanyContext.QueryAsync<WorkflowVersionStepsApiViewModel>(sql, dbArgs);

            workflowVersionSteps.ToList().ForEach(x => {
                x.Settings = new { settings = this.XmlToDynamic(x.SettingsXml), fields = this.XmlToDynamic(x.FieldsXml) };
            });
            return workflowVersionSteps;
        }

        public core.entities.Workflow.Type GetWorkflowTypeByUID(Guid workflowTypUid)
        {
            return this.CompanyContext.Filter<core.entities.Workflow.Type>(i => i.UID == workflowTypUid).SingleOrDefault();
        }

        public WorkflowVersion GetWorkflowVersionByUID(Guid workflowVerionUid)
        {
            return this.CompanyContext.Filter<WorkflowVersion>(i => i.UID == workflowVerionUid).SingleOrDefault();
        }

        public async Task<IEnumerable<WorkflowInstanceApiViewModel>> GetWorkflowInstances(Guid workflowUid)
        {
            var dbArgs = new DynamicParameters();
      
            dbArgs.Add("@uid", workflowUid);
            string sql = @" select Item.ID,
                                itemstep.UID,
                                vs.Name,
                                VS.State,
                                vs.StepType,
                                vs.ActivityType,
                                vs.Settings as SettingsXml,
                                vs.Fields as FieldsXml,
                                itemstep.Settings as Response1,
                                itemstep.Fields as Response2,
                                item.StartedOn,
                                item.CompletedOn,
                                R.uid as StartedByUid,
                                R1.uid as CompletedByUid
                                from workflow.[version]  v 
                                inner join  workflow.VersionStep  vs  on
                                vs.VersionID = v.id
                                inner join workflow.ItemStep itemstep on
                                itemstep.StepID = vs.id
                                inner join [workflow].[Item] item on
                                item.VersionID =v.id and itemstep.ItemID = item.id
                                left outer join reporting.Global_Resource R on R.ResourceID = item.StartedBy
                                left outer join reporting.Global_Resource R1 on R1.ResourceID = item.CompletedBy
                                 where item.uid=@uid";

            var workflowInstances = await this.CompanyContext.QueryAsync<WorkflowInstanceApiViewModel>(sql, dbArgs);

            workflowInstances.ToList().ForEach(x => {

                x.Settings = new { settings = this.XmlToDynamic(x.SettingsXml) , fields = this.XmlToDynamic(x.FieldsXml) };
                if (x.ActivityType == WorkflowActivityType.FieldChange)
                {
                    x.Responses = this.GetWorkFlowStepFieldChanges(this.XmlToDynamic(x.SettingsXml), x.ID);
                }else
                {
                    x.Responses = this.XmlToDynamic(x.Response2, false);
                }

                x.Assignments = this.GetWorkflowAssignments(this.XmlToDynamic(x.Response1));


            });
            return workflowInstances;
        }


        private IEnumerable<WorkflowAssignmentApiViewModel> GetWorkflowAssignments(dynamic settings)
        {
            IEnumerable<WorkflowAssignmentApiViewModel> assignment = new List<WorkflowAssignmentApiViewModel>();
            IList<string> resourceEmails = this.GetEmails(settings);
            if (resourceEmails.Count != 0)
                assignment =   this.GeWorkflowAssignmentApiViewModels(resourceEmails);
            
            return assignment;

        }

        private IEnumerable<WorkflowAssignmentApiViewModel> GeWorkflowAssignmentApiViewModels(IList<string> emails)
        {

            var sql = $@"select UID as AssigneeUid from reporting.Global_Resource  where email  IN ('{string.Join("','", emails)}')";
            var assignments =   this.CompanyContext.Query<WorkflowAssignmentApiViewModel>(sql).ToList();
            return assignments;

        }
        private List<string> GetEmails(dynamic settings)
        {
            List<string> resourceEmails = new List<string>();
            if (settings != null && settings.emails != null && settings.emails.email != null)
            {
                var emails = settings.emails;

                if (emails.email != null)
                {
                    if (emails.email.GetType().Name != "JArray")
                    {
                        emails.email = new JArray(emails.email);
                    }
                }


                for (int i = 0; i < emails.email.Count; i++)
                {
                    var e = emails.email[i];
                    string address = e["@address"].Value;

                    if (!resourceEmails.Any(r => r == address.ToLower()))
                        resourceEmails.Add(address.ToLower());
                }
            }
            return resourceEmails;
        }
        private List<WorkflowStepFieldChange> GetWorkFlowStepFieldChanges(dynamic setting,int itemId)
        {
            List<WorkflowStepFieldChange> fieldChanges = new List<WorkflowStepFieldChange>();
            if (setting != null && setting.FieldUpdate != null && setting.FieldUpdate.Field != null)
            {

                dynamic fields = new JArray(setting.FieldUpdate.Field);
                for (int i = 0; i < fields.Count; i++)
                {
                    var fieldChange = new WorkflowStepFieldChange();
                    bool isFromActionForm = false;


                    var field = fields[i];
                    int fieldTypeId = field["@FieldId"] != null ? field["@FieldId"] : 0;
                    if (fieldTypeId == 0) continue;
                    fieldChange.FormValue = field["@UseFormValue"] != null ? field["@UseFormValue"] : false;
                    fieldChange.ObjectType = field["@ObjectType"] != null ? field["@ObjectType"] : "";
                    fieldChange.UseCurrentDate = field["@UseCurrentDate"] != null ? field["@UseCurrentDate"] : false;
                    fieldChange.AppendValue = field["@AppendValue"] != null ? field["@AppendValue"] : "";
                    fieldChange.ClearValue = field["@ClearValue"] != null ? field["@ClearValue"] : "";
                    FieldType fieldType = this.CompanyContext.GetById<FieldType>(fieldTypeId);
                    fieldChange.FieldName = fieldType?.FriendlyName;
                    fieldChange.Type = fieldType?.Type;
                    string formFieldId = field["@FormFieldId"] != null ? field["@FormFieldId"] : null;
                    int stepId = field["@FormStepId"] != null ? field["@FormStepId"] : 0;
                    isFromActionForm = field["@IsActionForm"] != null ? bool.Parse(field["@IsActionForm"].ToString()) : false;

                    if (!isFromActionForm && fieldChange.FormValue && formFieldId != null && stepId != 0)
                    {

                        var stepSql = @"select fields from workflow.itemstep where  stepid=@stepid and itemid=@itemid";
                        dynamic stepFields = this.CompanyContext.Query<string>(stepSql, new { stepid = stepId, itemid = itemId }).FirstOrDefault();
                        stepFields = XmlToDynamic(stepFields, false);


                        if (stepFields.fields != null && stepFields.fields.form != null && stepFields.fields.form.Count > 1)
                        {
                            List<string> vlist = new List<string>();
                            string fieldValue = string.Empty;
                            for (int k = 0; k < stepFields.fields.form.Count; k++)
                            {
                                JArray sfields = new JArray(stepFields.fields.form[k].field);
                                JObject jo = sfields.Children<JObject>()
                                    .FirstOrDefault(o => o["@id"] != null && o["@id"].ToString() == formFieldId);
                                var displayvalue = jo != null && jo["@displayvalue"] != null ? jo["@displayvalue"].ToString() : "";
                                var fieldtype = jo != null && jo["@fieldtype"] != null ? jo["@fieldtype"].ToString() : "";
                                switch (fieldtype)
                                {
                                    case "date":
                                        fieldValue = displayvalue != "" ? Convert.ToDateTime(displayvalue).ToShortDateString() : "";
                                        break;
                                    default:
                                        if (fieldChange.AppendValue == "true")
                                            vlist.AddRange(displayvalue.Split(','));
                                        else
                                            fieldValue = displayvalue;
                                        break;
                                }
                            }

                            if (fieldChange.AppendValue == "true")
                                fieldChange.Value = string.Join(",", vlist.Distinct().ToArray());
                            else
                                fieldChange.Value = fieldValue;

                        }
                        else
                        {
                            JArray sfields = new JArray(stepFields.fields.form.field);
                            JObject jo = sfields.Children<JObject>()
                                .FirstOrDefault(o => o["@id"] != null && o["@id"].ToString() == formFieldId);
                            var displayvalue = jo != null && jo["@displayvalue"] != null ? jo["@displayvalue"].ToString() : "";
                            var fieldtype = jo != null && jo["@fieldtype"] != null ? jo["@fieldtype"].ToString() : "";
                            switch (fieldtype)
                            {
                                case "date":
                                    fieldChange.Value = displayvalue != "" ? Convert.ToDateTime(displayvalue).ToShortDateString() : "";
                                    break;
                                default:
                                    fieldChange.Value = displayvalue;
                                    break;
                            }
                        }
                    }

                    else
                        fieldChange.Value = field["@ValueLabel"] != null ? field["@ValueLabel"] : field["@Value"] != null ? field["@Value"] : "";

                    

                    fieldChanges.Add(fieldChange);
                }

            }

            return fieldChanges;
        }

        private dynamic XmlToDynamic(string xml, bool omitRootElement = true)
        {
            return XmlToObject<dynamic>(xml, omitRootElement);
        }

        private T XmlToObject<T>(string xml, bool omitRootElement = true)
        {
            return string.IsNullOrEmpty(xml) ? JsonConvert.DeserializeObject<T>("{}") : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeXNode(XElement.Parse(xml), Formatting.None, omitRootElement));
        }

        public WorkflowItem GetWorkflowItemByUID(Guid workflowItemUid)
        {
            return this.CompanyContext.Filter<WorkflowItem>(i => i.UID == workflowItemUid).SingleOrDefault();
        }

        private void gettWorkflowsQueryParamsSql(WorkflowsApiViewModel model, DynamicParameters dbArgs, List<string> whereClause, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            if (queryParams != null)
            {

                var orderBySql = "";
                var offsetSql = "";
                var pageNum = -1;
                var pageSize = model.pageSize != 0 ? model.pageSize : -1;



                queryParams.ToList().ForEach(x => {

                    switch (x.Key.ToLower())
                    {
                        case "actionuid":
                            Guid actionUid;

                            if ((Guid.TryParse(x.Value, out actionUid)) && (actionUid != Guid.Empty))
                            {
                                dbArgs.Add("@actionUid", actionUid);
                                whereClause.Add(" [DD].[UID] = @actionUid");
                            }
                            break;
                        case "assetuid":
                            Guid assetUid;
                            if ((Guid.TryParse(x.Value, out assetUid)) && (assetUid != Guid.Empty))
                            {
                                dbArgs.Add("@assetUid", assetUid);
                                whereClause.Add("[d].[UID] = @assetUid");
                            }
                            break;
                        case "relationshipuid":
                            Guid relationshipUid;
                            if ((Guid.TryParse(x.Value, out relationshipUid)) && (relationshipUid != Guid.Empty))
                            {
                                dbArgs.Add("@relationshipUid", relationshipUid);
                                whereClause.Add(" [DDD].[UID] = @relationshipUid");
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
                        case "versionuid":
                            Guid workflowVersionUid;
                            if (Enum.TryParse(x.Value, out workflowVersionUid))
                            {
                                dbArgs.Add("@workflowVersionUid", workflowVersionUid);
                                whereClause.Add(" [V].[UID] = @workflowVersionUid");
                            }
                            break;
                        case "state":
                            WorkflowApiState state;
                            if (Enum.TryParse(x.Value, out state))
                            {
                                if(state==WorkflowApiState.Active)
                                    whereClause.Add("item.CompletedOn is null");
                                else if(state == WorkflowApiState.InActive)
                                    whereClause.Add("item.CompletedOn is not null");
                            }
                            break;
                        case "_pagesize":
                            if (int.TryParse(x.Value, out pageSize))
                            {
                                if (pageSize < 1) pageSize = 1;
                            }
                            break;
                        case "_pagenum":
                            if (int.TryParse(x.Value, out pageNum))
                            {
                                if (pageNum < 1) pageNum = 1;
                            }
                            break;
                        case "_order":
                            switch (x.Value)
                            {
                                case "startedon":
                                    orderBySql = "order by item.StartedOn asc";
                                    break;
                                case "completedon":
                                    orderBySql = "order by item.CompletedOn asc";
                                    break;
                            }
                            break;

                    }


                });


                if (string.IsNullOrEmpty(orderBySql))
                {
                    orderBySql = "order by item.StartedOn asc";
                }

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

        public async Task<WorkflowsApiViewModel> GetWorkflows(IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            var dbArgs = new DynamicParameters();
            List<string> whereClause = new List<string>();
            List<string> pagingSql = new List<string>();
            WorkflowsApiViewModel model = new WorkflowsApiViewModel();


            this.gettWorkflowsQueryParamsSql(model, dbArgs, whereClause, pagingSql, queryParams);

            var whereSql = "";
            if (whereClause.Any())
                whereSql = $"where {string.Join(" and ", whereClause)}";

            string countSql = $@"select 
                count(*)
                        from workflow.type t
                    inner join workflow.version v on v.TypeID = t.Id
                    inner join  workflow.VersionStep  vs  on
                    vs.VersionID = v.id
                    inner join workflow.ItemStep itemstep on
                    itemstep.StepID = vs.id
                    inner join [workflow].[Item] item on
                    item.VersionID =v.id and itemstep.ItemID = item.id
                    left join Asset D on D.Object = item.Object and D.ObjectID = item.ObjectID 
                    left join issue iss on item.object = 'Issue' and iss.id = item.objectid
                    left join Asset DD on DD.Object = iss.Object and DD.ObjectID = iss.ObjectID 
                    left join [Intersect] inter on item.Object = 'Intersect' and item.objectid = inter.ID
                    left join Asset DDD on DDD.Object = inter.Object and DDD.ObjectID = inter.ObjectID 
                    left outer join reporting.Global_Resource R on R.ResourceID = item.StartedBy
                    left outer join reporting.Global_Resource R1 on R1.ResourceID = item.CompletedBy
				{whereSql}";

            string sql = $@"Select 
		        Item.UID as Uid,
		        case when item.[Object] = 'Issue' then DD.uid
		        ELSE NULL END as ActionUid ,
		        case when item.[Object] = 'Artifact' or item.[Object] = 'Rule' or item.[Object] = 'Policy'
		        or item.[Object] = 'Taxonomy' or item.[Object] = 'ShoppingCart' or  item.[Object] = 'ReferenceItem'
		        or item.[Object] = 'Fusion' then D.uid
		        ELSE NULL END as AssetUid,
		        case when item.[Object] = 'Intersect' then DDD.uid
		        ELSE NULL END as RelationshipUid,
		        t.Uid as WorkflowTypeUid,
		        V.UID as WorkflowVersionUid,
		        item.StartedOn,
		        item.CompletedOn,
		        R.uid as StartedByUid,
		        R1.uid as CompletedByUid
	        from workflow.type t
	        inner join workflow.version v on v.TypeID = t.Id
	        inner join  workflow.VersionStep  vs  on
	        vs.VersionID = v.id
	        inner join workflow.ItemStep itemstep on
	        itemstep.StepID = vs.id
	        inner join [workflow].[Item] item on
	        item.VersionID =v.id and itemstep.ItemID = item.id
	        left join Asset D on D.Object = item.Object and D.ObjectID = item.ObjectID 
	        left join issue iss on item.object = 'Issue' and iss.id = item.objectid
	        left join Asset DD on DD.Object = iss.Object and DD.ObjectID = iss.ObjectID 
	        left join [Intersect] inter on item.Object = 'Intersect' and item.objectid = inter.ID
	        left join Asset DDD on DDD.Object = inter.Object and DDD.ObjectID = inter.ObjectID 
	        left outer join reporting.Global_Resource R on R.ResourceID = item.StartedBy
	        left outer join reporting.Global_Resource R1 on R1.ResourceID = item.CompletedBy
				{whereSql}
                {string.Join("\n", pagingSql)}";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs);
            var count = countResults.First();

            var results = await CompanyContext.QueryAsync<WorkflowApiViewModel>(sql, dbArgs);

            model.items = results;
            model.total = count;

            return model;
        }
    }
}
