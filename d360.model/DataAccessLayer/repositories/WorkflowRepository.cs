using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using d360.core.entities;
using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.model.DataAccessLayer.repositories;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d360.model.DataAccessLayer
{
    public class WorkflowRepository : BaseRepository, IWorkflowRepository
    {
        private ICompanyContext CompanyContext;
        public WorkflowRepository(ICompanyContext CompanyContext)
            : base(CompanyContext)
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
                t.uid as 'WorkflowTypeUid',
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
					dc.DisplayValue as CreatedBy,
					t.UpdatedOn,
					du.DisplayValue as UpdatedBy,
					t.State as State,
					v.Version as PublishedVersion,
					is_t.name as ActionType,
					D.Name as AssetType,
					ITN.name as RelationshipType,
                    case when e.[Object] = 'ArtifactType' and D.[Class] = 1 then
						'Business Asset'
                    when e.[Object] = 'ArtifactType' and D.[Class] = 8 then
						'Technical Asset'
					when e.[Object] = 'RuleType' then
						'Rule'
					when e.[Object] = 'PolicyType' then
						'Policy'
					when e.[Object] = 'TaxonomyType' then
						'Model'
					when e.[Object] = 'IssueType' then
						'Action'
                    when e.[Object] = 'IntersectType' then
						'Relationship'
                    when e.[Object] = 'ShoppingCartType' then
                        'Shopping Cart'
					when e.[Object] = 'ReferenceItemType' then
					'Reference List'
					when e.[Object] = 'Fusion' then
						'Fusion'
					else
						''
					end as [Type]
				from workflow.type t
				inner join workflow.eventregistration e on e.typeid = t.id
				left join AssetType D on D.Object = E.Object and D.ObjectID = e.ObjectID 
				left join issuetype is_t on e.object = 'IssueType' and is_t.id = e.objectid
				left join IntersectType IT on e.Object = 'IntersectType' and e.objectid = IT.ID
                outer apply dbo.GetIntersectTypeNames(IT.ID) ITN
				left join workflow.version v on v.id = t.publishedversionid
				left join AssetDetail DC on DC.[Object] = 'Resource' and DC.ObjectID = t.CreatedBy
				left join AssetDetail DU on DU.[Object] = 'Resource' and DU.ObjectID = t.UpdatedBy
				{whereClause}
				order by t.Name asc";

            var workflowTypes = await CompanyContext.QueryAsync<WorkflowTypeApiViewModel>(sql, dbArgs, ApiTimeout);
            return workflowTypes;
        }

        public async Task<WorkflowVersionsApiViewModel> GetWorkflowVersions(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            List<string> whereClause = new List<string>();
            List<string> pagingSql = new List<string>();
            WorkflowVersionsApiViewModel model = new WorkflowVersionsApiViewModel();

            gettWorkflowVersionsQueryParamsSql(model, dbArgs, whereClause, pagingSql, queryParams);

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
				case when t.PublishedVersionID = V.Id then 1
					else 0 end as IsPublished,
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

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
            var count = countResults.First();

            var results = await CompanyContext.QueryAsync<WorkflowVersionApiViewModel>(sql, dbArgs, ApiTimeout);

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

            var workflowVersionSteps = await CompanyContext.QueryAsync<WorkflowVersionStepsApiViewModel>(sql, dbArgs, ApiTimeout);

            workflowVersionSteps.ToList().ForEach(x => {
                x.Settings = new { settings = XmlToDynamic(x.SettingsXml), fields = XmlToDynamic(x.FieldsXml) };
            });

            return workflowVersionSteps;
        }

        public core.entities.Workflow.Type GetWorkflowTypeByUID(Guid workflowTypUid)
        {
            return CompanyContext.Filter<core.entities.Workflow.Type>(i => i.UID == workflowTypUid).SingleOrDefault();
        }

        public WorkflowVersion GetWorkflowVersionByUID(Guid workflowVerionUid)
        {
            return CompanyContext.Filter<WorkflowVersion>(i => i.UID == workflowVerionUid).SingleOrDefault();
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
                                itemstep.Settings as ItemSettings,
                                itemstep.Fields as ItemFields,
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

            var workflowInstances = await CompanyContext.QueryAsync<WorkflowInstanceApiViewModel>(sql, dbArgs, ApiTimeout);

            workflowInstances.ToList().ForEach(x => {

                x.Settings = new { settings = XmlToDynamic(x.SettingsXml) , fields = XmlToDynamic(x.FieldsXml) };

                x.Responses = PopulateFieldResourceUids(XmlToDynamic(x.ItemFields, false));
                x.Assignments = GetWorkflowAssignments(XmlToDynamic(x.ItemSettings));

                if (x.ActivityType == WorkflowActivityType.FieldChange)
                {
                    x.Responses = GetWorkFlowStepFieldChanges(x.Responses, x.ID);
                }

                
            });


            return workflowInstances;
        }


        private IEnumerable<WorkflowAssignmentApiViewModel> GetWorkflowAssignments(dynamic settings)
        {
            IEnumerable<WorkflowAssignmentApiViewModel> assignment = new List<WorkflowAssignmentApiViewModel>();
            IList<string> resourceEmails = GetEmails(settings);
            if (resourceEmails.Count != 0)
                assignment =   GetWorkflowAssignmentApiViewModels(resourceEmails);
            
            return assignment;

        }

        private IEnumerable<WorkflowAssignmentApiViewModel> GetWorkflowAssignmentApiViewModels(IList<string> emails)
        {

            var sql = $@"select UID as AssigneeUid from reporting.Global_Resource  where email  IN ('{string.Join("','", emails)}')";
            var assignments = CompanyContext.Query<WorkflowAssignmentApiViewModel>(sql, timeout: ApiTimeout).ToList();
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

        private dynamic PopulateFieldResourceUids(dynamic fields)
        {
            var resources = new Dictionary<int, Guid?>();

            if (fields?.fields != null)
            {
                var forms = fields?.fields?.form;
                var reassignments = fields?.fields?.Reassigned;

                if (forms != null)
                {
                    forms = forms.GetType() == typeof(JArray) ? forms : new JArray(forms);

                    for (int i = 0; i < forms.Count; i++)
                    {
                        if (int.TryParse(forms[i]["@ResourceID"]?.ToString(), out int id) && !resources.ContainsKey(id))
                            resources.Add(id, null);
                    }
                }

                if (reassignments != null)
                {
                    reassignments = reassignments.GetType() == typeof(JArray) ? reassignments : new JArray(reassignments);

                    for (int i = 0; i < reassignments.Count; i++)
                    {
                        int id;
                        if (int.TryParse(reassignments[i]["@toResourceId"]?.ToString(), out id) && !resources.ContainsKey(id))
                            resources.Add(id, null);
                        if (int.TryParse(reassignments[i]["@fromResourceId"]?.ToString(), out id) && !resources.ContainsKey(id))
                            resources.Add(id, null);
                        if (int.TryParse(reassignments[i]["@byResourceId"]?.ToString(), out id) && !resources.ContainsKey(id))
                            resources.Add(id, null);
                    }
                }

                if (resources.Any())
                {
                    var guids = CompanyContext.Query<dynamic>($@"select [ResourceID], [uid] from reporting.Global_Resource where ResourceID in ({string.Join(",", resources.Keys.ToList())})").ToList();

                    if (guids.Any())
                    {
                        guids.ForEach(g =>
                        {
                            resources[g.ResourceID] = g.uid;
                        });
                    }

                    if (forms != null)
                    {
                        for (int i = 0; i < forms.Count; i++)
                        {
                            if (int.TryParse(forms[i]["@ResourceID"].ToString(), out int id))
                            {
                                forms[i]["@ResourceUid"] = resources[id];
                            }

                        }

                        fields.fields.form = forms;
                    }

                    if (reassignments != null)
                    {
                        for (int i = 0; i < reassignments.Count; i++)
                        {
                            int id;
                            if (int.TryParse(reassignments[i]["@toResourceId"]?.ToString(), out id))
                                reassignments[i]["@toResourceUid"] = resources[id];
                            if (int.TryParse(reassignments[i]["@fromResourceId"]?.ToString(), out id))
                                reassignments[i]["@fromResourceUid"] = resources[id];
                            if (int.TryParse(reassignments[i]["@byResourceId"]?.ToString(), out id))
                                reassignments[i]["@byResourceUid"] = resources[id];

                        }

                        fields.fields.Reassigned = reassignments;
                    }


                }

            }

            return fields;
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
                    FieldType fieldType = CompanyContext.GetById<FieldType>(fieldTypeId);
                    fieldChange.FieldName = fieldType?.FriendlyName;
                    fieldChange.Type = fieldType?.Type;
                    string formFieldId = field["@FormFieldId"] != null ? field["@FormFieldId"] : null;
                    int stepId = field["@FormStepId"] != null ? field["@FormStepId"] : 0;
                    isFromActionForm = field["@IsActionForm"] != null ? bool.Parse(field["@IsActionForm"].ToString()) : false;

                    if (!isFromActionForm && fieldChange.FormValue && formFieldId != null && stepId != 0)
                    {

                        var stepSql = @"select fields from workflow.itemstep where  stepid=@stepid and itemid=@itemid";
                        dynamic stepFields = CompanyContext.Query<string>(stepSql, new { stepid = stepId, itemid = itemId }).FirstOrDefault();
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
            return CompanyContext.Filter<WorkflowItem>(i => i.UID == workflowItemUid).SingleOrDefault();
        }

        private void getWorkflowsQueryParamsSql(WorkflowsApiViewModel model, DynamicParameters dbArgs, List<string> whereClause, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            if (queryParams != null)
            {

                var orderBySql = "";
                var offsetSql = "";
                var pageNum = -1;
                var pageSize = model.pageSize != 0 ? model.pageSize : -1;

                var defaultFilterIncuded = false;
                var direction = "asc";

                var directionParam = queryParams.FirstOrDefault(q => q.Key.ToLower() == "_direction");
                if (directionParam.Key != null)
                {
                    direction = directionParam.Value;
                }

                queryParams.ToList().ForEach(x => {

                    switch (x.Key.ToLower())
                    {
                        case "actionuid":
                            Guid actionUid;

                            if ((Guid.TryParse(x.Value, out actionUid)) && (actionUid != Guid.Empty))
                            {
                                dbArgs.Add("@actionUid", actionUid);
                                whereClause.Add(" [ISS].[UID] = @actionUid");
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
                                whereClause.Add(" [inter].[UID] = @relationshipUid");
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
                            if (Guid.TryParse(x.Value, out workflowVersionUid))
                            {
                                dbArgs.Add("@workflowVersionUid", workflowVersionUid);
                                whereClause.Add(" [V].[UID] = @workflowVersionUid");
                            }
                            break;
                        case "active":
                            WorkflowApiState state;
                            if (Enum.TryParse(x.Value, out state))
                            {
                                defaultFilterIncuded = true;
                                if (state==WorkflowApiState.Active)
                                    whereClause.Add("item.CompletedOn is null");
                                else 
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
                            switch (x.Value.ToLower())
                            {
                                case "startedon":
                                    orderBySql = $"order by item.StartedOn {direction}";
                                    break;
                                case "completedon":
                                    orderBySql = $"order by item.CompletedOn {direction}";
                                    break;
                            }
                            break;

                    }


                });

                if (!defaultFilterIncuded)
                    whereClause.Add("item.CompletedOn is null");

                if (string.IsNullOrEmpty(orderBySql))
                {
                    orderBySql = $"order by item.StartedOn {direction}";
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


            getWorkflowsQueryParamsSql(model, dbArgs, whereClause, pagingSql, queryParams);

            var whereSql = "";
            if (whereClause.Any())
                whereSql = $"where {string.Join(" and ", whereClause)}";

            string countSql = $@"select 
                count(*)
                        from workflow.type t
                    inner join workflow.version v on v.TypeID = t.Id
                    inner join [workflow].[Item] item on item.VersionID = v.id
                    left join Asset D on D.Object = item.Object and D.ObjectID = item.ObjectID 
                    left join issue iss on item.object = 'Issue' and iss.id = item.objectid
                    left join [Intersect] inter on item.Object = 'Intersect' and item.objectid = inter.ID
                    left outer join reporting.Global_Resource R on R.ResourceID = item.StartedBy
                    left outer join reporting.Global_Resource R1 on R1.ResourceID = item.CompletedBy
				{whereSql}";

            string sql = $@"Select 
		        Item.UID as Uid,
		        case when item.[Object] = 'Issue' then iss.uid
		        ELSE NULL END as ActionUid ,
		        case when item.[Object] = 'Artifact' or item.[Object] = 'Rule' or item.[Object] = 'Policy'
		        or item.[Object] = 'Taxonomy' or item.[Object] = 'ShoppingCart' or  item.[Object] = 'ReferenceItem'
		        or item.[Object] = 'Fusion' then D.uid
		        ELSE NULL END as AssetUid,
		        case when item.[Object] = 'Intersect' then inter.uid
		        ELSE NULL END as RelationshipUid,
		        t.Uid as WorkflowTypeUid,
		        V.UID as WorkflowVersionUid,
		        item.StartedOn,
		        item.CompletedOn,
		        R.uid as StartedByUid,
		        R1.uid as CompletedByUid
	        from workflow.type t
	        inner join workflow.version v on v.TypeID = t.Id
	        inner join [workflow].[Item] item on item.VersionID =v.id
	        left join Asset D on D.Object = item.Object and D.ObjectID = item.ObjectID 
	        left join issue iss on item.object = 'Issue' and iss.id = item.objectid
	        left join [Intersect] inter on item.Object = 'Intersect' and item.objectid = inter.ID
	        left outer join reporting.Global_Resource R on R.ResourceID = item.StartedBy
	        left outer join reporting.Global_Resource R1 on R1.ResourceID = item.CompletedBy
				{whereSql}
                {string.Join("\n", pagingSql)}";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
            var count = countResults.First();

            var results = await CompanyContext.QueryAsync<WorkflowApiViewModel>(sql, dbArgs, ApiTimeout);

            model.items = results;
            model.total = count;

            return model;
        }

        public async Task<IEnumerable<WorkflowReassignmentAssetTypeApiModel>> GetWorkflowReassignmentAssetTypes(int workflowItemId)
        {
            var allowedAssetTypeClasses = new List<AssetTypeClass>
            { 
                AssetTypeClass.BusinessAsset,
                AssetTypeClass.TechnicalAsset,
                AssetTypeClass.Model,
                AssetTypeClass.Rule,
                AssetTypeClass.Policy
            };

            var assetTypeClasses = AssetTypeClass.BusinessAsset.GetAsList().Where(a => allowedAssetTypeClasses.Contains(a.ID));

            var assetTypeClassSql = $" AT.Class in ({string.Join(", ", assetTypeClasses.Select(a => (int)a.ID))})";
            var assetTypeClassNameSql = $@"case {string.Join("\n", assetTypeClasses.Select(a => $" when AT.Class = {(int)a.ID} then '{a.Name}' "))} else '' end";

            var sql = $@"select AT.ID, ATP.Path as Name, AT.Object, AT.ObjectID, {assetTypeClassNameSql} as AssetClassName, ATP.Path + ' :: ' + {assetTypeClassNameSql} as Label from workflow.item I
                            inner join workflow.Version V on V.ID = I.VersionID
                            inner join Issue S on S.ID = I.ObjectID and I.Object = 'Issue'
                            inner join IssueType IT on IT.ID = S.IssueTypeID
                            cross apply (
	                            select	E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObject""]/@Value)[1]', 'nvarchar(max)') as IssueObject,
                                        E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObjectID""]/@Value)[1]', 'int') as IssueObjectID
                                from workflow.EventRegistration E where E.TypeID = V.TypeID
                            ) IC
                            inner join AssetType AT on AT.Object = IC.IssueObject and AT.ObjectID = IC.IssueObjectID
                            cross apply dbo.GetAssetTypeTextPathById(AT.ID,' > ') ATP
                            where I.ID = @id and {assetTypeClassSql}

                            union

                            select AT.ID, ATP.Path as Name, AT.Object, AT.ObjectID, {assetTypeClassNameSql} as AssetClassName, ATP.Path + ' :: ' + {assetTypeClassNameSql} as Label from AssetType AT
                            inner join workflow.item I on I.ID = @id
                            inner join workflow.Version V on V.ID = I.VersionID
                            inner join Issue S on S.ID = I.ObjectID and I.Object = 'Issue'
                            inner join IssueType IT on IT.ID = S.IssueTypeID
                            cross apply(select count(*) as Allocations from IssueTypeRelation ITR where ITR.IssueTypeID = IT.ID) C
                            cross apply(
                               select  E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObject""]/@Value)[1]', 'nvarchar(max)') as IssueObject
                               from workflow.EventRegistration E where E.TypeID = V.TypeID
                            ) IC
                            cross apply dbo.GetAssetTypeTextPathById(AT.ID,' > ') ATP
                            where C.Allocations = 0 and IC.IssueObject is null and {assetTypeClassSql}

                            union

                            select AT.ID, ATP.Path as Name, AT.Object, AT.ObjectID, {assetTypeClassNameSql} as AssetClassName, ATP.Path + ' :: ' + {assetTypeClassNameSql} as Label from workflow.item I
                            inner join workflow.Version V on V.ID = I.VersionID
                            inner join Issue S on S.ID = I.ObjectID and I.Object = 'Issue'
                            inner join IssueType IT on IT.ID = S.IssueTypeID
                            inner join IssueTypeRelation ITR on ITR.IssueTypeID = IT.ID
                            inner join AssetType AT on AT.ID = ITR.AssetTypeID
                            cross apply(
                                select E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObject""]/@Value)[1]', 'nvarchar(max)') as IssueObject
                                from workflow.EventRegistration E where E.TypeID = V.TypeID
                            ) IC
                            cross apply dbo.GetAssetTypeTextPathById(AT.ID,' > ') ATP
                            where I.ID = @id and IC.IssueObject is null and {assetTypeClassSql}

                            order by 5,2";

            return await CompanyContext.QueryAsync<WorkflowReassignmentAssetTypeApiModel>(sql, new { id = workflowItemId });
        }

        public async Task<IEnumerable<WorkflowReassignmentAssetApiModel>> GetWorkflowReassignmentAssets(int assetTypeId, string query, int resultCount = 1000)
        {
            var sql = $@"select top {resultCount} AP.ID, 
                                AP.DisplayPath as Name, 
                                A.Object, 
                                A.ObjectID 
                        from graph.AssetNodeDisplayPath AP
                        inner join Asset A on A.ID = AP.ID
                        where AP.AssetTypeID = @assetTypeId {(string.IsNullOrWhiteSpace(query) ? "" : "and AP.DisplayPath like '%' + @query + '%'")}
                        order by AP.DisplayPath";


            return await CompanyContext.QueryAsync<WorkflowReassignmentAssetApiModel>(sql, new { assetTypeId, query });
        }

    }
}
