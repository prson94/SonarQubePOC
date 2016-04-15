using d360.core;
using d360.model;
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

        public JsonNetResult HomeSocial()
        {
            return new JsonNetResult { Data = Company.GetSocialDataForCurrentResource(), Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GroupSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForGroup(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult ProfileSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForResource(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult RelationshipAggregates(SystemObjects type, int id)
        {
            return new JsonNetResult { Data = Company.GetAggregateRelationshipBreakdownsByObject(type, id), Formatting = Newtonsoft.Json.Formatting.None };
        }
        
        public JsonNetResult FollowingBreakdownByResource(int id)
        {
            var query = Company.Query<dynamic>(@"select  		                
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

        public JsonNetResult ResponsibilityBreakdownByResource(int id)
        {
            var query = Company.Query<dynamic>(
            @"            
            select		            
		            T.ObjectType as [Type], 
		            T.ObjectTypeName as TypeName, 
		            T.ObjectTypeID as TypeID, 		
		            T.[Count],
		            coalesce(S.IconBackColor, '#000') as IconBackColor,
                    coalesce(S.IconForeColor, '#fff') as IconForeColor,
                    coalesce(S.IconText, substring(T.ObjectTypeName, 1, 2)) as IconText
            from (
			            select	O.ObjectType, 
					            O.ObjectTypeName, 
					            O.ObjectTypeID, 		
					            count(1) as [Count]
			            from (
				            select	ObjectName, 
						            ObjectType, 
						            ObjectTypeName, 
						            case ObjectType 
							            when 'Policy' then 0 
							            when 'Rule' then 0 
							            else ObjectTypeID 
						            end as ObjectTypeID
				            from ResponsibilityDetailForResource
				            where ResponsibleObjectType = 'Resource' and ResponsibleObjectID = @r and Visible = 1 and ObjectTypeName is not null
				            group by ObjectName, 
						            ObjectType, ObjectTypeName, 		case ObjectType 
							            when 'Policy' then 0 
							            when 'Rule' then 0 
							            else ObjectTypeID 
						            end
				            ) O
			            group by O.ObjectType, O.ObjectTypeID, O.ObjectTypeName
			            ) T
			            left join ObjectStyle S on  T.ObjectType + 'Type' = S.ObjectType and T.ObjectTypeID = S.ObjectID order by TypeName

            ", new { r = id });

                    return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
    }
}