using d360.core;
using d360.model;
using d360.web.Models.Attributes;
using Dapper;
using System.Linq;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("queries"), Authorize, AiHandleError]
    public class QueriesController : BaseController
    {
        #region DI

        public QueriesController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        [Route("{id:int}/UsedVsUnusedResponsibilitiesByArtifactType")]
        public JsonNetResult UsedVsUnusedResponsibilitiesByArtifactType(int id)
        {
            var query = Company.Query<dynamic>(@"select	T.Name as Responsibility, 
		AT.Name as ArtifactType, 
		AT.ID as ArtifactTypeID,
		coalesce(O.[Count], 0) as [AssignedCount], 
		OT.Total - coalesce(O.[Count], 0) as UnassignedCount, 
		OT.Total
from	ResponsibilityType T
		inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeID = T.ID and T.ResponsibilityTypeGroup = 1
		inner join ArtifactType AT on R.ObjectType = 'ArtifactType' and AT.ID = @id and AT.ID = R.ObjectID
		cross apply (
					select	coalesce(count(1), 0) as Total
					from	Artifact
					where	ArtifactTypeID = AT.ID
					) OT
		left join	(
					select		RD.ResponsibilityTypeID,
								RD.ObjectType,
								RD.ObjectTypeID,
								count(1) as [Count]
					from		(
								select		RD.ResponsibilityTypeID,
											RD.ObjectType,
											RD.ObjectTypeID,
											RD.ObjectID
								from		ResponsibilityDetail RD
								where		RD.ObjectType = 'Artifact'
								group by	RD.ResponsibilityTypeID,
											RD.ObjectType,
											RD.ObjectTypeID,
											RD.ObjectID
								) RD
					group by	RD.ResponsibilityTypeID,
								RD.ObjectType,
								RD.ObjectTypeID
					) O on O.ResponsibilityTypeID = T.ID and O.ObjectTypeID = AT.ID
order by	AT.Name,
			T.Name", new { id = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
        
        [Route("FollowingByResourceByType"), NonNullableParameters]
        public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
        {
            var query = Company.Query<dynamic>(@"select ObjectType, ObjectID, Name, ID, Url, CurrentScore, OpenEventCount
from FollowDetail
where ResourceID = @r and Type = @t and TypeID = @i", new { r = resourceID, t = new Dapper.DbString { Value = type, IsAnsi = true }, i = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
        
        [Route("ResponsibilityTypeBreakdown"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeBreakdown()
        {
            var query = Company.Query<dynamic>(@"
select		RD.ResponsibilityTypeID,
			RD.ResponsibilityTypeName as ResponsibilityType,
			count(1) as [Count]
from		(
			select		RD.ResponsibilityTypeID,
						RD.ResponsibilityTypeName,
						RD.ResourceID
			from		ResponsibilityDetails RD
						inner join reporting.Global_Resource R on R.ResourceID = RD.ResourceID
			group by	RD.ResponsibilityTypeID,
						RD.ResponsibilityTypeName,
						RD.ResourceID
			) RD
group by	RD.ResponsibilityTypeID,
			RD.ResponsibilityTypeName");

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{id:int}/ResourcesByResponsibilityType")]
        public JsonNetResult GetResourcesByResponsibilityType(int id)
        {
            var query = Company.Query<dynamic>(@"
select		RD.ResourceID,
			R.FirstName,
			R.LastName,
			RD.ResponsibilityTypeID,
			count(1) as OwnedItemCount
from		ResponsibilityDetails RD
			inner join reporting.Global_Resource R on R.ResourceID = RD.ResourceID
where		RD.ResponsibilityTypeID = @id
group by	RD.ResourceID,
			R.FirstName,
			R.LastName,
			RD.ResponsibilityTypeID
order by	R.LastName, R.FirstName", new { id = id }).ToList();
            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

//        [Route("{resourceID:int}/{responsibilityTypeID:int}/ResponsibilitiesByResource")]
//        public JsonNetResult GetResponsibilitiesByResource(int resourceID, int responsibilityTypeID)
//        {
//            var query = Company.Query<dynamic>(@"
//select		ObjectType,
//			ObjectID,
//			ObjectName,
//			ObjectTypeName,
//			ObjectUrl,
//			CurrentScore,
//			ResponsibleObjectID as OwnerID 
//from		ResponsibilityDetailForResource
//where		Visible = 1 
//			and ResponsibilityTypeID = @r
//			and ResponsibleObjectID = @id 
//group by	ObjectType,
//			ObjectID,
//			ObjectName,
//			ObjectTypeName,
//			ObjectUrl,
//			CurrentScore,
//			ResponsibleObjectID
//order by	ObjectTypeName, ObjectName", new { id = resourceID, r = responsibilityTypeID }).ToList();

//            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
//        }

        [Route("{type}/{id:int}/ScoreHistoryByObject")]
        public JsonNetResult GetScoreHistoryByObject(SystemObjects type, int id)
        {
            var query = Company.Query<dynamic>(@"EXEC GetScoreHistoryByObject @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, id = id });
            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{type}/{id:int}/AverageScoreByObjectType")]
        public JsonNetResult GetAverageScoreByObjectType(SystemObjects type, int id)
        {
            var query = Company.Query<dynamic>(@"EXEC GetAverageScoreByObjectType @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, id = id }).SingleOrDefault();
            return new JsonNetResult{ Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{type}/{id:int}/PointBreakdownByObject")]
        public JsonNetResult GetPointBreakdownByObject(SystemObjects type, int id)
        {
            var query = Company.Query<dynamic>(@"
	select	M.ID,
			G.Name + ': ' + I.Name as Name,
			MR.Value
	from	metrics.Score S
			inner join metrics.MapResult MR on MR.ScoreID = S.ID
			inner join metrics.Map M on M.ID = MR.MapID
			inner join metrics.[Group] G on G.ID = M.GroupID
			inner join metrics.ITem I on I.ID = M.ItemID
	where	getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate
			and S.Object = @type and S.ObjectID = @id", 
            new {
                type = new DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 },
                id = id
            });

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