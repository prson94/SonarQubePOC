using d360.core;
using d360.model;
using d360.web.Models.Attributes;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("tiles"), Authorize]
    public class TilesController : BaseController
    {        
        #region DI

        public TilesController(CommunityContext community, CompanyContext company) 
            : base(community, company)
        { 
        }

        #endregion

        [Route("HomeSocial")]
        public JsonNetResult HomeSocial()
        {
            return new JsonNetResult { Data = Company.GetSocialDataForCurrentResource(), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("GroupSocial"), NonNullableParameters]
        public JsonNetResult GroupSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForGroup(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ProfileSocial"), NonNullableParameters]
        public JsonNetResult ProfileSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForResource(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("RelationshipAggregates"), NonNullableParameters]
        public JsonNetResult RelationshipAggregates(SystemObjects type, int id)
        {
            return new JsonNetResult { Data = Company.GetAggregateRelationshipBreakdownsByObject(type, id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("FollowingBreakdownByResource"), NonNullableParameters]
        public async Task<JsonNetResult> FollowingBreakdownByResource(int id)
        {
            var query = await Company.QueryAsync<dynamic>(@"select  		                
		                T.[Type], 
		                T.TypeName,
		                T.TypeID, 		
		                T.[Count],
		                coalesce(S.IconBackColor, '#000') as IconBackColor,
                        coalesce(S.IconForeColor, '#fff') as IconForeColor,
                        coalesce(S.IconText, substring(T.TypeName, 1, 2)) as IconText
                from (
                select 
	                [Type], 
	                TypeName, 
	                TypeID, 
	                count(1) as [Count]
                from 
	                FollowDetail
                where 
	                ResourceID = @r
	                and ObjectType not in ('ArtifactType', 'PolicyType', 'ReferenceItemType', 'ResourceType', 'TaxonomyType')
                group by Type, TypeName, TypeID) T
                left join ObjectStyle S on  T.[Type] = S.ObjectType and T.TypeID = S.ObjectID order by TypeName", new { r = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ResponsibilityBreakdownByResource"), NonNullableParameters]
        public async Task<JsonNetResult> ResponsibilityBreakdownByResource(int id, int? responsibilityTypeID)
        {

            var sql = $@"
		select		{QueryConstants.HighLevelTypeCaseStatement} + T.Name as TypeName,
					C.[Type],
					C.TypeID,
					count(1) as [Count]
		from		[cache].[AssetResponsibility] C
					inner join AssetType T on T.Object = C.Type and T.ObjectID = C.TypeID
					left join dbo.OrganizationResource OrRe on C.SecurityAsset = 'O' and OrRe.OrganizationID = C.SecurityAssetID
					left join dbo.Organization Org on C.SecurityAsset = 'O' and Org.ID = OrRe.OrganizationID
					left join dbo.ResourceGroup ReGr on C.SecurityAsset = 'G' and ReGr.GroupID = C.SecurityAssetID
		where		C.IsVisible = 1 and C.Overriden = 0 
                    {(responsibilityTypeID.HasValue ? "and C.ResponsibilityTypeID = @rt" : "")}	
					and coalesce(OrRe.ResourceID, ReGr.ResourceID, C.SecurityAssetID) = @r
		group by	{QueryConstants.HighLevelTypeCaseStatement} + T.Name,
					C.[Type],
					C.TypeID,
					coalesce(OrRe.ResourceID, ReGr.ResourceID, C.SecurityAssetID);";

            var query = await Company.QueryAsync<dynamic>(sql, new { r = id, rt = responsibilityTypeID });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ResponsibilityBreakdownByGroup"), NonNullableParameters]
        public async Task<JsonNetResult> ResponsibilityBreakdownByGroup(int id)
        {
            var sql = $@"            
select		RD.Type,
			RD.TypeID,
			{QueryConstants.HighLevelTypeCaseStatement} + T.Name as TypeName,
			count(1) as [Count]
from		[cache].[AssetResponsibility] RD 
			inner join AssetType T on T.Object = RD.Type and T.ObjectID = RD.TypeID and RD.SecurityAsset = 'G' and RD.SecurityAssetID = @id
group by    RD.Type, 
            RD.TypeID, 
            { QueryConstants.HighLevelTypeCaseStatement} + T.Name 
order by    { QueryConstants.HighLevelTypeCaseStatement} + T.Name";

            var query = await Company.QueryAsync<dynamic>(sql, new { id });


            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
    }
}