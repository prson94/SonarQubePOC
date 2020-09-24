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

            var assetSQL = "";
            var issueTypeSQL = "";
            var orderBy = $"Order by Name";

            List<string> conditions = new List<string>();                        

            var baseIssueTypesSql = $@"Select 
                                        IT.Uid, 
                                        IT.Name, 
                                        IT.Description, 
                                        IT.IsSystem, 
                                        IT.UpdatedOn, 
                                        IT.UpdatedBy 
                                    from 
                                        IssueType IT";        

            issueTypeSQL = baseIssueTypesSql;

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

            #region Asset and Asset Type


            var assetTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assettypeuid");

            var assetUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assetuid");

            if (assetTypeUidParam.Key != null || assetUidParam.Key != null)
            {
                List<string> assetConditions = new List<string>();
                var joinSQL = $@"Inner Join IssueTypeRelation ITR on IT.ID = ITR.IssueTypeID
                             Inner Join AssetType AT on AT.ID = ITR.AssetTypeID
                             Inner Join Asset A on A.AssetTypeID = AT.ID";

                issueTypeSQL = $@"{baseIssueTypesSql}
                                    cross apply (select count(*) as Allocations from IssueTypeRelation R where R.IssueTypeID = IT.ID) C
                                    where C.Allocations = 0";
                
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

                var assetConditionStr = "";
                if(assetConditions.Count >0)
                {
                    assetConditionStr = $"Where { string.Join(" AND ", assetConditions.ToArray())}";

                    assetSQL = $@" UNION 
                                {baseIssueTypesSql} 
                                {joinSQL}
                                {assetConditionStr}";
                }                
            }

            #endregion

            var conditionStr = conditions.Count > 0 ? string.Join(" AND ", conditions.ToArray()) : "";

            if (conditionStr.Trim() != "" && assetSQL.Trim() == "")
            {
                issueTypeSQL = $"{issueTypeSQL} Where ";
            }

            if(assetSQL.Trim() != "")
            {
                assetSQL = $@"{assetSQL}
                              {conditionStr}
                              {orderBy}";
            }

            var sql = $@"{issueTypeSQL} 
                         {conditionStr}
                         {orderBy}
                         {assetSQL}";
            
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
