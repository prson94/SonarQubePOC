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
using d360.core.resources;

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
            bool hasAssetParam = false;
            bool limitToActiveWorkflows = false;

            List<string> issueConditions = new List<string>();                        
            List<string> assetConditions = new List<string>();                                              

            List<string> issueJoins = new List<string>();
            List<string> assetJoins = new List<string>();

            var assetSql = "";
            var resourceSql = "";
            var issueTypeSql = "";

            var orderBySql = $"Order by Name";

            var baseIssueTypesSql = $@"Select 
                                        IT.Uid, 
                                        IT.Name, 
                                        IT.Description, 
                                        IT.IsSystem, 
                                        IT.UpdatedOn,
                                        R.Uid as UpdatedByUid
                                    from 
                                        IssueType IT
                                        left join [reporting].[Global_Resource] R on R.ResourceID = IT.UpdatedBy ";


            var workflowSql = $@"EXISTS (SELECT 1 FROM workflow.type T INNER JOIN workflow.EventRegistration E on E.TypeID = T.ID and E.[Object] = 'IssueType' and E.ObjectID = IT.ID and T.State = 1)";
            var workflowObjectSql = $@"EXISTS (SELECT 1 FROM workflow.type T INNER JOIN workflow.EventRegistration E on E.TypeID = T.ID and E.[Object] = 'IssueType' and E.ObjectID = IT.ID and T.State = 1
									WHERE (E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObjectID""]/@Value)[1]', 'int') IS NULL) 
                                    OR ((E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObject""]/@Value)[1]', 'nvarchar(max)') = AT.[Object] 
                                    AND E.Condition.value('(./Conditions/Condition[@ContextualFieldID=""IssueObjectID""]/@Value)[1]', 'int') = AT.ObjectID)))";

            issueTypeSql = baseIssueTypesSql;

            assetConditions.Add("1 = 1");
            issueConditions.Add("1 = 1");

            var limitToActiveWorkflowsParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_limittoactiveworkflows");
            var resourceUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_resourceuid");
            var assetTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assettypeuid");
            var assetUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assetuid");
            var actionTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_actiontypeuid");
            var nameParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_name");


            hasAssetParam = !string.IsNullOrWhiteSpace(assetTypeUidParam.Value) || !string.IsNullOrWhiteSpace(assetUidParam.Value);

            #region Action Type

            if (actionTypeUidParam.Key != null)
            {
                if (actionTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(actionTypeUidParam.Value) && (Guid.TryParse(actionTypeUidParam.Value, out Guid actionTypeUid) && actionTypeUid != Guid.Empty))
                {
                    issueConditions.Add("IT.uid = @actionTypeUid");
                    assetConditions.Add("IT.uid = @actionTypeUid");
                    dbArgs.Add("@actionTypeUid", actionTypeUid);
                }
                else
                {
                    throw new ArgumentException(IssueErrors.InvalidActionUid);
                }
            }

            #endregion

            #region Name

            if (nameParam.Key != null)
            {
                if (!string.IsNullOrWhiteSpace(nameParam.Value))
                {
                    issueConditions.Add("IT.Name = @name");
                    assetConditions.Add("IT.Name = @name");
                    dbArgs.Add("@name", nameParam.Value);
                }
            }

            #endregion

            #region Limit By Active Workflows

            if (limitToActiveWorkflowsParam.Key != null)
            {
                if (limitToActiveWorkflowsParam.Value != null && !string.IsNullOrWhiteSpace(limitToActiveWorkflowsParam.Value) && bool.TryParse(limitToActiveWorkflowsParam.Value, out limitToActiveWorkflows))
                {
                    if (limitToActiveWorkflows)
                    {
                        var activeWorkflowSql = hasAssetParam ? workflowObjectSql : workflowSql;

                        issueConditions.Add(activeWorkflowSql);
                        assetConditions.Add(workflowObjectSql);
                    }

                }
                else
                {
                    throw new ArgumentException(IssueErrors.InvalidLimitProvided);
                }
            }
            #endregion

            #region Asset, Asset Type and Resource

            if (actionTypeUidParam.Key != null)
            {
                issueTypeSql = $@"{baseIssueTypesSql}
                                  {string.Join("\n", issueJoins)}
                                  where {string.Join(" AND ", issueConditions)}";
            }

            if (assetTypeUidParam.Key != null || assetUidParam.Key != null || resourceUidParam.Key != null)
            {
                
                assetJoins.Add("inner Join IssueTypeRelation ITR on IT.ID = ITR.IssueTypeID");
                assetJoins.Add("inner Join AssetType AT on AT.ID = ITR.AssetTypeID");
                assetJoins.Add("inner Join Asset A on A.AssetTypeID = AT.ID");

                issueJoins.Add("cross apply (select count(*) as Allocations from IssueTypeRelation R where R.IssueTypeID = IT.ID) C");

                if (!string.IsNullOrEmpty(assetTypeUidParam.Value))
                {
                    issueJoins.Add("left join AssetType AT on AT.uid = @assetTypeUid");
                }
                else if (!string.IsNullOrEmpty(assetUidParam.Value))
                {
                    issueJoins.Add("left join Asset A on A.uid = @assetUid");
                    issueJoins.Add("left join AssetType AT on AT.ID = A.AssetTypeID");
                }

                issueConditions.Add("C.Allocations = 0");

                issueTypeSql = $@"{baseIssueTypesSql}
                                  {string.Join("\n", issueJoins)}
                                  where {string.Join(" AND ", issueConditions)}";

                
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
                        throw new ArgumentException(IssueErrors.InvalidActionUid);
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
                        throw new ArgumentException(IssueErrors.InvalidActionUid);
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
                        throw new ArgumentException(IssueErrors.InvalidResourceUid);
                    }
                }
               
                if (hasAssetParam || hasResourceParam)
                {
                    if (hasResourceParam)
                    {
                        resourceSql = $@"UNION 
                                {baseIssueTypesSql} 
                                {string.Join("\n", assetJoins)}
                                inner Join IssueTypeRelationResponsibility RR on ITR.ID = RR.IssueTypeRelationID
	                            inner join ResponsibilityDetail RD on RD.ResponsibilityTypeID = RR.ResponsibilityTypeId and RD.ResourceUid = @resourceUid and ((RD.AssetID = A.ID) or (RD.AssetTypeID = A.AssetTypeID and RD.AssetID = 0))
                                where {string.Join(" AND ", assetConditions)}";

                        assetConditions.Add("RR.ID is null");

                        assetJoins.Add("left join IssueTypeRelationResponsibility RR on RR.IssueTypeRelationID = ITR.ID");
                    }

                    assetSql = $@" UNION 
                                {baseIssueTypesSql} 
                                {string.Join("\n", assetJoins)}
                                where {string.Join(" AND ", assetConditions)}";
                }                
            }
            else if (issueConditions.Any())
            {
                issueTypeSql = $@"{baseIssueTypesSql}
                                  {string.Join("\n", issueJoins)}
                                  where {string.Join(" AND ", issueConditions)}";
            }
            #endregion                     

            var sql = $@"{issueTypeSql} 
                         {assetSql}
                         {resourceSql}
                         {orderBySql}";

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
