using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;
using Dapper;
using d360.model.DataAccessLayer.repositories;

namespace d360.model.DataAccessLayer
{
    public class IssueRepository : BaseRepository, IIssueRepository
    {
        ICompanyContext companyContext;
        public IssueRepository(ICompanyContext context)
            :base(context)
        {
            this.companyContext = context;
        }

        private Expression<Func<IssueType, IssueTypeApiModel>> 
            apiModelMapper = x => new IssueTypeApiModel() { Description = x.Description, IsSystem = x.IsSystem, Name = x.Name, UpdatedOn = x.UpdatedOn, Uid = x.uid };

        public async Task<IEnumerable<IssueTypeApiModel>> GetIssueTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
        {           

            var dbArgs = new DynamicParameters();

            bool hasResourceParam = false;
            bool limitToActiveWorkflows = false;
            var assetSQL = "";
            var resourceSQL = "";
            var issueTypeSQL = "";
            var orderBy = $"Order by Name";

            List<string> conditions = new List<string>();                        

            var baseIssueTypesSql = $@"Select 
                                        IT.Uid, 
                                        IT.Name, 
                                        IT.Description, 
                                        IT.IsSystem, 
                                        IT.UpdatedOn,
                                        R.Uid as UpdatedByUid
                                    from 
                                        IssueType IT
                                        LEFT JOIN 
                                        [reporting].[Global_Resource] R on R.ResourceID = IT.UpdatedBy ";

            var workflowSql = $@"EXISTS (SELECT 1 FROM workflow.type T INNER JOIN workflow.EventRegistration E on E.TypeID = T.ID and E.[Object] = 'IssueType' and E.ObjectID = IT.ID and T.State = 1)";
            var workflowObjectSql = $@"EXISTS (SELECT 1 FROM workflow.type T INNER JOIN workflow.EventRegistration E on E.TypeID = T.ID and E.[Object] = 'IssueType' and E.ObjectID = IT.ID and T.State = 1
									WHERE (E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObjectID""]/@Value)[1]', 'int') IS NULL) 
                                    OR ((E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObject""]/@Value)[1]', 'nvarchar(max)') = AT.[Object] 
                                    AND E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObjectID""]/@Value)[1]', 'int') = AT.ObjectID)))";

            issueTypeSQL = baseIssueTypesSql;

            #region Limit By Active Workflows
            var limitToActiveWorkflowsParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_limittoactiveworkflows");

            if (limitToActiveWorkflowsParam.Key != null)
            {
                if (limitToActiveWorkflowsParam.Value != null && !string.IsNullOrWhiteSpace(limitToActiveWorkflowsParam.Value) && bool.TryParse(limitToActiveWorkflowsParam.Value, out limitToActiveWorkflows))
                {

                }
                else
                {
                    throw new ArgumentException("Invalid Limit To Active Workflows value provided");
                }
            }
            #endregion

            #region Action Type
            var actionTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_actiontypeuid");

            if (actionTypeUidParam.Key != null)
            {
                if (actionTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(actionTypeUidParam.Value) && (Guid.TryParse(actionTypeUidParam.Value, out Guid actionTypeUid) && actionTypeUid != Guid.Empty))
                {
                    conditions.Add("IT.uid = @actionTypeUid");
                    dbArgs.Add("@actionTypeUid", actionTypeUid);
                }
                else
                {
                    throw new ArgumentException("Invalid Action type uid value provided");
                }
            }

            #endregion

            #region Name

            var nameParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_name");

            if (nameParam.Key != null)
            {
                if (!string.IsNullOrWhiteSpace(nameParam.Value))
                {
                    conditions.Add("IT.Name = @name");
                    dbArgs.Add("@name", nameParam.Value);
                }
            }

            #endregion

            #region Asset, Asset Type and Resource

            var resourceUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_resourceuid");

            var assetTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assettypeuid");

            var assetUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assetuid");

            if (assetTypeUidParam.Key != null || assetUidParam.Key != null || resourceUidParam.Key != null)
            {
                List<string> assetConditions = new List<string>();
                var joinSQL = $@"Inner Join IssueTypeRelation ITR on IT.ID = ITR.IssueTypeID
                             Inner Join AssetType AT on AT.ID = ITR.AssetTypeID
                             Inner Join Asset A on A.AssetTypeID = AT.ID";

                var activeWorkflowSql = string.IsNullOrEmpty(assetTypeUidParam.Value) ? workflowSql : workflowObjectSql;
                issueTypeSQL = $@"{baseIssueTypesSql}
                                    cross apply (select count(*) as Allocations from IssueTypeRelation R where R.IssueTypeID = IT.ID) C
                                    {(string.IsNullOrEmpty(assetTypeUidParam.Value) ? "" : "left join AssetType AT on AT.uid = @assetTypeUid")}
                                    where C.Allocations = 0
                                    {(limitToActiveWorkflows ? "and " + activeWorkflowSql : "")}";
                
                if (assetTypeUidParam.Key != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value))
                {                 
                    if (Guid.TryParse(assetTypeUidParam.Value, out Guid assetTypeUid))
                    {
                        if (assetTypeUid != Guid.Empty)
                        {                            
                            assetConditions.Add("AT.Uid = @assetTypeUid");

                            dbArgs.Add("@assetTypeUid", assetTypeUid);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Invalid Action type uid value provided");
                    }
                }                

                if (assetUidParam.Key != null && !string.IsNullOrWhiteSpace(assetUidParam.Value))
                {
                    if (Guid.TryParse(assetUidParam.Value, out Guid assetUid))
                    {
                        if (assetUid != Guid.Empty)
                        {
                            assetConditions.Add("A.Uid = @assetUid");

                            dbArgs.Add("@assetUid", assetUid);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Invalid Action type uid value provided");
                    }
                }

                if (resourceUidParam.Key != null && !string.IsNullOrWhiteSpace(resourceUidParam.Value))
                {
                    if (Guid.TryParse(resourceUidParam.Value, out Guid resourceUid))
                    {
                        if (resourceUid != Guid.Empty)
                        {
                            hasResourceParam = true;
                            dbArgs.Add("@resourceUid", resourceUid);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Invalid resource uid value provided");
                    }
                }

                var assetConditionStr = "";                
                if (assetConditions.Count > 0 || hasResourceParam)
                {
                    var resourceJoinSQL = "";

                    assetConditionStr += "Where " + (limitToActiveWorkflows ? workflowObjectSql : "1=1");

                    if (assetConditions.Count > 0)
                    {                        
                        assetConditionStr = $"AND { string.Join(" AND ", assetConditions.ToArray())}";                        
                    }

                    if (hasResourceParam)
                    {
                        resourceSQL = $@" UNION 
                                {baseIssueTypesSql} 
                                {joinSQL}
                                Inner Join IssueTypeRelationResponsibility RR on ITR.ID=RR.IssueTypeRelationID
	                            inner join ResponsibilityDetail RD on RD.ResponsibilityTypeID=RR.ResponsibilityTypeId and RD.ResourceUid=@resourceUid and ((RD.AssetID = A.ID) or (RD.AssetTypeID = A.AssetTypeID and RD.AssetID = 0))
                                {assetConditionStr}";

                        assetConditionStr += string.IsNullOrWhiteSpace(assetConditionStr) ? " where RR.ID is null" : " and RR.ID is null";

                        resourceJoinSQL = "left join IssueTypeRelationResponsibility RR on RR.IssueTypeRelationID = ITR.ID";
                    }

                    assetSQL = $@" UNION 
                                {baseIssueTypesSql} 
                                {joinSQL}
                                {resourceJoinSQL}
                                {assetConditionStr}";
                }                
            }

            #endregion
                      
            var conditionStr = conditions.Count > 0 ? string.Join(" AND ", conditions.ToArray()) : "";

            if (conditionStr.Trim() != "")
            {
                if(assetSQL.Trim() == "")
                {
                    issueTypeSQL = $"{issueTypeSQL} Where ";
                }
                else
                {
                    conditionStr = $"AND {conditionStr}";
                }                
            }            

            if(assetSQL.Trim() != "")
            {
                assetSQL = $@"{assetSQL}
                              {conditionStr}
                              {(string.IsNullOrEmpty(conditionStr) ? $"WHERE " : "AND")} {(limitToActiveWorkflows ? workflowObjectSql : "")}";
            }

            if (resourceSQL.Trim() != "")
            {
                resourceSQL = $@"{resourceSQL}
                                {conditionStr}
                                {(string.IsNullOrEmpty(conditionStr) ? $"WHERE " : "AND")} {(limitToActiveWorkflows ? workflowObjectSql : "")}";
            }

            var sql = $@"{issueTypeSQL} 
                         {conditionStr}
                         {assetSQL}
                         {resourceSQL}
                         {orderBy}";

            return await this.companyContext.QueryAsync<IssueTypeApiModel>(sql, dbArgs, ApiTimeout);
        }

        public async Task<IEnumerable<IssueTypeApiModel>> GetAllocationByAssetType(Guid uid)
        {
            var dbArgs = new DynamicParameters();
            string whereClause = " where T.uid= @uid";
            dbArgs.Add("@uid", uid);

            string sql = $@"select I.uid,I.Name,I.Description,I.IsSystem,I.UpdatedOn
                from IssueTypeRelation R
                inner join AssetType T on T.ID = R.AssetTypeID
                inner join IssueType I on I.ID = R.IssueTypeID                
                {whereClause}";

            var allocations = await this.companyContext.QueryAsync<IssueTypeApiModel>(sql, dbArgs, ApiTimeout);
            return allocations;
        }

        public IssueType GetIssueTypeByUID(Guid issueTypeUid)
        {
            return this.companyContext.Filter<IssueType>(i => i.uid == issueTypeUid).SingleOrDefault();
        }

        public Issue GetIssueByUID(Guid issueUid)
        {
            return this.companyContext.Filter<Issue>(i => i.UID == issueUid).SingleOrDefault();
        }
    }
}
