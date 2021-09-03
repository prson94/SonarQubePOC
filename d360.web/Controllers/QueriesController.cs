using d360.core;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Models.Attributes;
using Dapper;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("queries"), Authorize, AiHandleError]
    public class QueriesController : BaseController
    {
        #region DI

        public QueriesController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        { }

        #endregion

        [Route("FollowingByResourceByType"), NonNullableParameters]
        public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
        {
            var query = Company.Query<dynamic>(@"select ObjectType, ObjectID, Name, ID, Url, CurrentScore, OpenEventCount
from FollowDetail
where ResourceID = @r and Type = @t and TypeID = @i and Type != ObjectType", new { r = resourceID, t = new Dapper.DbString { Value = type, IsAnsi = true }, i = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
        
        [Route("ResponsibilityTypeBreakdown"), NonNullableParameters]
        public async Task<JsonNetResult> GetResponsibilityTypeBreakdown()
        {
            var query = await Company.QueryAsync<dynamic>(@"exec [dbo].[GetResponsibilityTypeBreakdown]");

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{uid:Guid}/ResourcesByResponsibilityType")]
        public JsonNetResult GetResourcesByResponsibilityType(Guid uid)
        {
            int responsibilityTypeID = Company.ResponsibilityTypes.Where(t => t.UID == uid).Select(t => t.ID).First();
            return GetResourcesByResponsibilityType(responsibilityTypeID);
        }

        [Route("{id:int}/ResourcesByResponsibilityType")]
        public JsonNetResult GetResourcesByResponsibilityType(int id)
        {
            var query = Company.Query<dynamic>(@"

select		OC.ResourceID,
			R.FirstName,
			R.LastName,
			OC.ResponsibilityTypeID,
			sum(OC.[Count] * OC.AssetCount) as OwnedItemCount
from		(
			select		ResponsibilityTypeID,
						ResourceID,
						count(1) as [Count],
						C.Count as AssetCount
			from		ResponsibilityDetail R
			cross apply (
				select 
						case when R.ApplyToType = 1 and R.AssetID = 0 then 
							(select count(*) from Asset where AssetTypeID = R.AssetTypeID) 
						else 
							1
				end as [Count]
			) C
			where		IsVisible = 1
						and ResponsibilityTypeID = @id
			group by	ResponsibilityTypeID,
						ResourceID,
						C.Count
			) OC
			inner join reporting.Global_Resource R on R.ResourceID = OC.ResourceID
group by	OC.ResourceID,
			R.FirstName,
			R.LastName,
			OC.ResponsibilityTypeID
order by	R.LastName, R.FirstName", new { id }).ToList();
            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{type}/{id:int}/SocialBreakdown")]
        public JsonNetResult GetSocialBreakdownByObject(string type, int id)
        {
            var query = Company.Query<dynamic>(@"
select 'followers' as Suffix, count(1) as [Count], 'Followers' as Name
from	Follow
where	ObjectType = @type and ObjectID = @id
union
select 'comments' as Suffix, count(1) as [Count], 'Comments' as Name
from	Comment C
		inner join CommentRelation R	on R.CommentID = C.ID 
										and R.ObjectType = @type 
										and R.ObjectID = @id
                                        and C.ParentID is null", new { type = new Dapper.DbString { Value = type, IsAnsi = true, IsFixedLength = true, Length = 50}, id = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
    }
}