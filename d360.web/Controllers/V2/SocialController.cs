using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Description;
using System.Web.Mvc;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/social"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class SocialController : BaseApiController
    {
        public SocialController(CommunityContext community, CompanyContext company) : base(community, company)
        {
        }

        [
            HttpGet,
            Route("FollowingBreakdownByResource"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of FollowingBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
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
	                and ObjectType not in ('ArtifactType', 'PolicyType', 'ReferenceItemType', 'ResourceType', 'TaxonomyType')
                group by Type, TypeName, TypeID) T
                left join ObjectStyle S on  T.[Type] = S.ObjectType and T.TypeID = S.ObjectID order by TypeName", new { r = id });
            
            return Request.CreateResponse(HttpStatusCode.OK, query);
        }

        [
            HttpGet,
            Route("ResponsibilityBreakdownByResource"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of ResponsibilityBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters]
        public async Task<HttpResponseMessage> ResponsibilityBreakdownByResource(int id, int? responsibilityTypeID)
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
			                where		C.IsVisible = 1 
						                 {(responsibilityTypeID.HasValue ? "and C.ResponsibilityTypeID = @rt" : "")}
						                and C.ResourceID = @r
			                group by C.[Type], C.TypeID, A.Count
		                ) R on R.[Type] = T.Object and R.TypeID = T.ObjectID
						";

            var query = await Company.QueryAsync<dynamic>(sql, new { r = id, rt = responsibilityTypeID });
            
            return Request.CreateResponse(HttpStatusCode.OK, query);
        }

        [
            HttpGet,
            Route("ResponsibilityBreakdownByGroup"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of ResponsibilityBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            NonNullableParameters]
        public async Task<HttpResponseMessage> ResponsibilityBreakdownByGroup(int id)
        {
            var sql = $@"            
select		RD.Type,
			RD.TypeID,
			{QueryConstants.HighLevelTypeCaseStatement} + T.Name as TypeName,
			count(1) as [Count]
from		ResponsibilityDetail RD 
			inner join AssetType T on T.ID = RD.AssetTypeID and RD.SecurityAsset = 'G' and RD.SecurityAssetID = @id and RD.IsVisible = 1
group by    RD.Type, 
            RD.TypeID, 
            { QueryConstants.HighLevelTypeCaseStatement} + T.Name 
order by    { QueryConstants.HighLevelTypeCaseStatement} + T.Name";

            var query = await Company.QueryAsync<dynamic>(sql, new { id });
            
            return Request.CreateResponse(HttpStatusCode.OK, query);
        }
    }
}