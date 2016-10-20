using d360.core;
using d360.model;
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

        [Route("GroupSocial")]
        public JsonNetResult GroupSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForGroup(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ProfileSocial")]
        public JsonNetResult ProfileSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForResource(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("RelationshipAggregates")]
        public JsonNetResult RelationshipAggregates(SystemObjects type, int id)
        {
            return new JsonNetResult { Data = Company.GetAggregateRelationshipBreakdownsByObject(type, id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("FollowingBreakdownByResource")]
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
	                and ObjectType not in ('ArtifactType', 'DomainType', 'DomainGroup', 'PolicyType', 'ResourceType', 'TaxonomyType')
                group by Type, TypeName, TypeID) T
                left join ObjectStyle S on  T.[Type] = S.ObjectType and T.TypeID = S.ObjectID order by TypeName", new { r = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ResponsibilityBreakdownByResource")]
        public async Task<JsonNetResult> ResponsibilityBreakdownByResource(int id)
        {            
            var query = await Company.QueryAsync<dynamic>(
            @"            
            select
					O.ObjectType as [Type],
					O.ObjectTypeName as TypeName,
					O.ObjectTypeID as TypeID,
					O.Count as [Count],
					coalesce(S.IconBackColor, '#000') as IconBackColor,
					coalesce(S.IconForeColor, '#fff') as IconForeColor,
					coalesce(S.IconText, substring(O.ObjectTypeName, 1, 2)) as IconText
				from(
						select	r.ObjectType, 
						        r.ObjectTypeName, 
						        case r.ObjectType 
							            when 'Policy' then 0 
							            when 'Rule' then 0 
							            else r.ObjectTypeID 
						        end as ObjectTypeID,													            
								count(1) as [Count]
				            from ResponsibilityDetailForResource r							
				            where ResponsibleObjectType = 'Resource' and ResponsibleObjectID = @r and Visible = 1 and ObjectTypeName is not null
				            group by r.ObjectType, r.ObjectTypeName, ObjectTypeID) O
				left join ObjectStyle S on  O.ObjectType + 'Type' = S.ObjectType and O.ObjectTypeID = S.ObjectID order by typename
            ", new { r = id });

                    return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ResponsibilityBreakdownByGroup")]
        public async Task<JsonNetResult> ResponsibilityBreakdownByGroup(int id)
        {
            var query = await Company.QueryAsync<dynamic>(
            @"            
            select
					O.ObjectType as [Type],
					O.ObjectTypeName as TypeName,
					O.ObjectTypeID as TypeID,
					O.Count as [Count],
					coalesce(S.IconBackColor, '#000') as IconBackColor,
					coalesce(S.IconForeColor, '#fff') as IconForeColor,
					coalesce(S.IconText, substring(O.ObjectTypeName, 1, 2)) as IconText
				from(
						select	r.ObjectType, 
						        r.ObjectTypeName, 
						        case r.ObjectType 
							            when 'Policy' then 0 
							            when 'Rule' then 0 
							            else r.ObjectTypeID 
						        end as ObjectTypeID,													            
								count(1) as [Count]
				            from ResponsibilityDetail r							
				            where ResponsibleObjectType = 'Group' and ResponsibleObjectID = @r and Visible = 1 and ObjectTypeName is not null
				            group by r.ObjectType, r.ObjectTypeName, ObjectTypeID) O
				left join ObjectStyle S on  O.ObjectType + 'Type' = S.ObjectType and O.ObjectTypeID = S.ObjectID order by typename
            ", new { r = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
    }
}