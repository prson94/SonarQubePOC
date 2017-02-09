using d360.core;
using d360.model;
using d360.web.Models.Attributes;
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

        [Route("{id:int}/StatusBreakdownByArtifactType")]
        public JsonNetResult StatusBreakdownByArtifactType(int id)
        {
            var query = Company.Query<dynamic>(@"select		Status, 
			count(1) as [Count],
			case Status 
				when 'Certified' then '#3f9d40'
				when 'Draft' then '#d32f2f'
				else '#e2792a'
			end as BackColor 
from		Artifact 
where		ArtifactTypeID = @id
group by	Status
order by	Status ", new { id = id });

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
            var query = Company.Query<dynamic>(@"select O.ResponsibilityType, O.ResponsibilityTypeID, count(1) as [Count]
from	(
		select	distinct O.ResponsibilityType, O.ResponsibilityTypeID, COALESCE(RG.ResourceID, O.ResponsibleObjectID) as ResourceID
		from	[cache].[ResponsibilityItem] O
				left join ResourceGroup RG on O.ResponsibleObject = 'Group' and RG.GroupID = O.ResponsibleObjectID
		where	O.ResponsibilityTypeGroup = 1
                and COALESCE(RG.ResourceID, O.ResponsibleObjectID) in (select ResourceID from reporting.Global_Resource)
		) O
group by	O.ResponsibilityType, O.ResponsibilityTypeID");

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{id:int}/ResourcesByResponsibilityType")]
        public JsonNetResult GetResourcesByResponsibilityType(int id)
        {
            var query = Company.Query<dynamic>(@"
select		R.ResponsibilityTypeID, 
			R.ResponsibilityType, 
			R.ResourceID,
			R.FirstName,
			R.LastName,
			C.OwnedItemCount
from		(
			select		R.ResponsibilityTypeID, 
						R.[Role] as ResponsibilityType, 
						R.ResponsibleObjectID as ResourceID,
						U.FirstName,
						U.LastName
			from		ResponsibilityDetailForResource R
						inner join reporting.Global_Resource U on U.ResourceID = R.ResponsibleObjectID and R.Visible = 1 and R.ResponsibilityTypeID = @id  
			group by	R.[Role], 
						R.ResponsibilityTypeID, 
						R.ResponsibleObjectID,
						U.FirstName,
						U.LastName
			) R
			inner join	(
						select	ResponsibilityTypeID,
								ResponsibleObjectID,
								count(1) as OwnedItemCount
						from	(
								select		ResponsibilityTypeID,
											ResponsibleObjectID,
											ObjectType,
											ObjectID
								from		ResponsibilityDetailForResource
								where		Visible = 1 
								group by	ResponsibilityTypeID,
											ResponsibleObjectID,
											ObjectType,
											ObjectID
								) C
						group by	ResponsibilityTypeID,
									ResponsibleObjectID
						) C on C.ResponsibilityTypeID = R.ResponsibilityTypeID and C.ResponsibleObjectID = R.ResourceID
            order by R.FirstName, R.LastName", new { id = id }).ToList();
            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{resourceID:int}/{responsibilityTypeID:int}/ResponsibilitiesByResource")]
        public JsonNetResult GetResponsibilitiesByResource(int resourceID, int responsibilityTypeID)
        {
            var query = Company.Query<dynamic>(@"
select		ObjectType,
			ObjectID,
			ObjectName,
			ObjectTypeName,
			ObjectUrl,
			CurrentScore,
			ResponsibleObjectID as OwnerID 
from		ResponsibilityDetailForResource
where		Visible = 1 
			and ResponsibilityTypeID = @r
			and ResponsibleObjectID = @id 
group by	ObjectType,
			ObjectID,
			ObjectName,
			ObjectTypeName,
			ObjectUrl,
			CurrentScore,
			ResponsibleObjectID
order by	ObjectTypeName, ObjectName", new { id = resourceID, r = responsibilityTypeID }).ToList();

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

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
            var query = Company.Query<dynamic>(@"select T.Name, T.Score as MaxScore, coalesce(S.Score, 0) as Score
from	StatisticType T
inner join cache.Object D on D.[Object] = @type and D.ObjectID = @id and D.ObjectType = T.[Object]  and T.ObjectID = D.ObjectTypeID and T.PartOfScore = 1
outer apply (
			select	top 1
					*
			from	Statistic
			where	StatisticTypeID = T.ID
					and ObjectType = @type
					and ObjectID = @id
			order by DateStart desc
			) S
order by	T.Name", new { type = type.ToString(), id = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("Rule/{id:int}/EventStatusBreakdown")]
        public JsonNetResult GetEventStatusBreakdownByRule(int id, int? maxHistoryDays)
        {
            var sql = "";

            if (maxHistoryDays.HasValue)
            {
                sql = @"select	Status, count(1) as [Count] from Event E inner join EventGroup G on G.ID = E.EventGroupID and G.RuleID = @id where DATEDIFF(dd, E.[Date], getutcdate()) <= @m group by Status";
            }
            else
            {
                sql = @"select	Status, count(1) as [Count] from Event E inner join EventGroup G on G.ID = E.EventGroupID and G.RuleID = @id group by Status";
            }

            var query = Company.Query<dynamic>(sql, new { id = id, m = maxHistoryDays });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("Rule/{id:int}/EventAgeBreakdown")]
        public JsonNetResult GetEventAgeBreakdownByRule(int id, int? maxHistoryDays)
        {
            var whereClause = "";

            if (maxHistoryDays.HasValue)
            {
                whereClause = @" where DATEDIFF(dd, E.[Date], getutcdate()) <= @m ";
            }
            var query = Company.Query<dynamic>(string.Format(@"select	cast(E.[Date] as Date) as [Date],
					count(1) as [Count]
			from	Event E
					inner join EventGroup G on G.ID = E.EventGroupID and G.RuleID = @id 
            {0}
            group by cast(E.[Date] as Date)
			order by cast(E.[Date] as Date)", whereClause), new { id = id, m = maxHistoryDays });
            
            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("Rule/{id:int}/EventCriticalityBreakdown")]
        public JsonNetResult GetEventCriticalityBreakdownByRule(int id, int? maxHistoryDays)
        {
            var whereClause = "";

            if (maxHistoryDays.HasValue)
            {
                whereClause = @" where DATEDIFF(dd, E.[Date], getutcdate()) <= @m ";
            }

            var query = Company.Query<dynamic>(string.Format(@"
select		Criticality,
			count(1) as [Count]
from		(
			select	case E.Criticality
						when 5 then 'Critical'
						when 4 then 'High'
						when 3 then 'Medium'
						when 2 then 'Low'
						else 'Negligible'
					end as Criticality,
					E.Criticality as SortOrder
			from	Event E
					inner join EventGroup G on G.ID = E.EventGroupID and G.RuleID = @id 
            {0}
			) o
group by	Criticality, SortOrder
order by	SortOrder desc", whereClause), new { id = id, m = maxHistoryDays });

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
                                        and C.ParentID is null", new { type = new Dapper.DbString { Value = type, IsAnsi = true }, id = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("RelatedArtifacts"), NonNullableParameters]
        public JsonNetResult RelatedArtifacts(int artifactID)
        {
            var query = Company.Query<dynamic>(@"select TA.*, 
dbo.GenerateObjectUrl('Artifact', TA.ArtifactTypeID, TA.ID) as Url
from	RelatedArtifact SR
		inner join RelatedArtifact TR on TR.GroupID = SR.GroupID and SR.ArtifactID = @artifactID
		inner join Artifact TA on TR.ArtifactID = TA.ID and TA.ID <> SR.ArtifactID", new { artifactID = artifactID });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("RelatedArtifactOptions"), NonNullableParameters]
        public JsonNetResult RelatedArtifactOptions(int typeID, int artifactID)
        {
            var query = Company.Query<dynamic>(@"if not exists(select GroupID from RelatedArtifact where ArtifactID = @a)
begin
	select		A.ID,
				A.Name
	from		Artifact A
	where		A.ArtifactTypeID = @t and A.ID <> @a
				and (
					A.ID in (select ObjectID from ResponsibilityDetailForResource where ObjectType = 'Artifact' and ResponsibleObjectID = @r)
					or
					@admin = 1
					) 
	order by	A.Name
end
else
begin
	select		A.ID,
				A.Name
	from		Artifact A
				left join RelatedArtifact SA on SA.ArtifactID = A.ID
	where		A.ArtifactTypeID = @t
				and SA.GroupID is null
				and (
					A.ID in (select ObjectID from ResponsibilityDetailForResource where ObjectType = 'Artifact' and ResponsibleObjectID = @r)
					or
					@admin = 1
					) 
	order by	A.Name
end", new { t = typeID, a = artifactID, r = Company.CurrentResourceID, admin = Company.CurrentResourceIsAdmin });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }
    }
}