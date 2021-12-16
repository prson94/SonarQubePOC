using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System;
using System.Linq;
using System.Data.Entity;
using d360.model.DataAccessLayer;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/social"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class SocialController : BaseV2ApiController
    {
        public SocialController(CoreComponentSet set): base(set)
        {
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("FollowingBreakdownByResource"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of FollowingBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            NonNullableParameters
        ]
        public async Task<HttpResponseMessage> FollowingBreakdownByResource(int id)
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
        	                and ObjectType not in ('ArtifactType', 'PolicyType', 'ReferenceItemType', 'ResourceType', 'TaxonomyType', 'RuleType')
                        group by Type, TypeName, TypeID) T
                        left join AssetType A on A.[Object] = T.[Type] and A.ObjectID = T.TypeID
                        left join AssetTypeStyle S on S.ID = A.ID
                        order by TypeName", new { r = id }, ApiTimeout);

            return Request.CreateResponse(HttpStatusCode.OK, query);
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("ResponsibilityBreakdownByResource"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of ResponsibilityBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> ResponsibilityBreakdownByResource(int id, Guid responsibilityTypeUID)
        {
            //Use responsibilityTypeID in the query for efficieny - the Type is primary key and ResponsibilityDetail a union view
            int? responsibilityTypeID = null;
            if (responsibilityTypeUID != Guid.Empty)
            {
                responsibilityTypeID = Company.ResponsibilityTypes.Where(t => t.UID == responsibilityTypeUID).Select(t => t.ID).FirstOrDefault();
            }
            return await ResponsibilityBreakdownByResource(id, responsibilityTypeID).ConfigureAwait(false);
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("ResponsibilityBreakdownByResource"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of ResponsibilityBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> ResponsibilityBreakdownByResource(int id, int? responsibilityTypeID = null)
        {

            var sql = $@"
select  
 {QueryConstants.HighLevelTypeCaseStatement} + T.Name as TypeName,
 R.Type,
 R.TypeID,
 R.AssetCount as [Count]
 from AssetType T
 inner join (
     select 
		C.[Type],
		C.TypeID,
		AC.Count as AssetCount
     from ResponsibilityDetail C
	cross apply (
		SELECT count(*) as 'Count' from ( 
			select 
				AssetID
			from 
			ResponsibilityDetail RD 
			inner join AssetType T on T.ObjectID = RD.TypeID and T.Object = RD.Type and T.Object = C.Type and T.ObjectID = C.TypeID
			inner join Asset A on A.AssetTypeID = T.ID
			where 
				ResourceID = @r and AssetID = 0 and ApplyToType = 1 and RD.IsVisible = 1
				{(responsibilityTypeID.HasValue ? " and RD.ResponsibilityTypeID = @rt" : "")}
		
			union all
				select
				AssetID
			from	ResponsibilityDetail RD
					inner join AssetType T on T.Object = RD.Type and T.ObjectID = RD.TypeID and RD.ResourceID = @r and T.Object = C.Type and T.ObjectID = C.TypeID
			where  
				RD.ApplyToType = 0 and RD.IsVisible = 1
				{(responsibilityTypeID.HasValue ? " and RD.ResponsibilityTypeID = @rt" : "")}
		) A
	) AC(Count)
     where
		C.IsVisible = 1 and C.ResourceID = @r
		{(responsibilityTypeID.HasValue ? " and C.ResponsibilityTypeID = @rt" : "")}
 ) R on R.[Type] = T.Object and R.TypeID = T.ObjectID
 Group by T.Object, T.Class, t.[Name], R.[Type], r.TypeID, R.AssetCount
";

            var query = await Company.QueryAsync<dynamic>(sql, new { r = id, rt = responsibilityTypeID }, ApiTimeout);
            return Request.CreateResponse(HttpStatusCode.OK, query);
        }

        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("ResponsibilityBreakdownByGroup"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of ResponsibilityBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            NonNullableParameters]
        public async Task<HttpResponseMessage> ResponsibilityBreakdownByGroup(int id)
        {
            var sql = $@"select  
        		                    {QueryConstants.HighLevelTypeCaseStatement} + T.Name as TypeName,
        			                			                 R.Type,
        			                 R.TypeID,
        			                 R.[Count] * R.AssetCount as [Count]
        		                from AssetType T
        		                inner join (
        			                select 
        						                C.[Type],
        						                C.TypeID,
        						                count(1) as [Count],
        										A.Count as AssetCount
        			                from ResponsibilityDetail C
        							cross apply (
        								select 
        										case when C.ApplyToType = 1 and C.AssetID = 0 then 
        											(select count(*) from Asset where AssetTypeID = C.AssetTypeID) 
        										else 
        											1
        								end as [Count]
        							) A
        			                where		C.IsVisible = 1 and C.SecurityAsset = 'G' and C.SecurityAssetID = @id
        			                group by C.[Type], 
                                            C.TypeID, A.Count
        		                ) R on R.[Type] = T.Object and R.TypeID = T.ObjectID
                                order by    { QueryConstants.HighLevelTypeCaseStatement} + T.Name
        						";
        
            var query = await Company.QueryAsync<dynamic>(sql, new { id }, ApiTimeout);
            
            return Request.CreateResponse(HttpStatusCode.OK, query);
        }
    }
}