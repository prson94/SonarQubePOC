using d360.core;
using d360.core.entities;
using d360.model;
using d360.web.Models.Formatters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace d360.web.Controllers
{
    [RoutePrefix("queries"), Authorize]
    public class QueriesController : BaseController
    {
        #region DI

        public QueriesController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

//        [Route("CriticalNonCriticalRedFlagAggregates")]
//        public JsonNetResult GetCriticalNonCriticalRedFlagAggregates()
//        {
//            var query = Company.Query<dynamic>(
//@"select D.ObjectType as Type,
//D.ObjectTypeName as TypeName,
//D.ObjectTypeID as TypeID,
//count(1) as [Count],
//sum(C.CriticalCount) as [CriticalCount]
//from		AlertFlag AF
//			inner join cache.ObjectDetails D on D.[Object] = AF.ObjectType and D.ObjectID = AF.ObjectID 
//			cross apply (
//						select	case 
//									when count(1) > 0 then 1 
//									else 0
//								end as CriticalCount
//						from	Relationship R 
//						where	R.SourceType = AF.ObjectType
//								and R.SourceObjectID = AF.ObjectID
//								and R.Classification = 1 -- CRITICAL
//						) C
//where		AF.Active = 1
//group by	D.ObjectType, D.ObjectTypeName, D.ObjectTypeID
//order by	D.ObjectTypeName");
//            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
//        }

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
        
        [Route("FollowingBreakdownByResource")]
        public JsonNetResult GetFollowingBreakdownByResource(int id)
        {
            var query = Company.Query<dynamic>(@"select Type, TypeName, TypeID, count(1) as [Count]
from FollowDetail
where ResourceID = @id
and ObjectType not in ('ArtifactType', 'DomainType', 'DomainGroup', 'PolicyType', 'ResourceType', 'TaxonomyType')
group by Type, TypeName, TypeID", new { id = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("FollowingByResourceByType")]
        public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
        {
            var query = Company.Query<dynamic>(@"select ObjectType, ObjectID, Name, ID, Url, CurrentScore, OpenEventCount
from FollowDetail
where ResourceID = @r and Type = @t and TypeID = @i", new { r = resourceID, t = type, i = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ResponsibilityBreakdownByResource")]
        public JsonNetResult GetResponsibilityBreakdownByResource(int id)
        {
            var query = Company.Query<dynamic>(
@"select	ObjectType, 
		ObjectTypeName, 
		ObjectTypeID, 
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
	where ResponsibleObjectType = 'Resource' and ResponsibleObjectID = @id and Visible = 1 and ObjectTypeName is not null
	group by ObjectName, 
			ObjectType, ObjectTypeName, 		case ObjectType 
				when 'Policy' then 0 
				when 'Rule' then 0 
				else ObjectTypeID 
			end
	) O
group by ObjectType, ObjectTypeID, ObjectTypeName", new { id = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ResponsibilityTypeBreakdown")]
        public JsonNetResult GetResponsibilityTypeBreakdown()
        {
            var query = Company.Query<dynamic>(@"select O.ResponsibilityType, O.ResponsibilityTypeID, count(1) as [Count]
from	(
		select	distinct T.Name as ResponsibilityType, T.ID as ResponsibilityTypeID, COALESCE(RG.ResourceID, O.ResponsibleObjectID) as ResourceID
		from	Responsibility O
				inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID and T.ResponsibilityTypeGroup = 1
				left join ResourceGroup RG on O.ResponsibleObjectType = 'Group' and RG.GroupID = O.ResponsibleObjectID
		) O
group by	O.ResponsibilityType, O.ResponsibilityTypeID");

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{id:int}/ResourcesByResponsibilityType")]
        public JsonNetResult GetResourcesByResponsibilityType(int id)
        {
            var query = Company.Query<dynamic>(@"select O.ResponsibilityTypeID, coalesce(RG.ResourceID, O.ResponsibleObjectID) as ResourceID, R.FirstName, R.LastName, COUNT(1) as OwnedItemCount
			from	Responsibility O
					left join ResourceGroup RG on O.ResponsibleObjectType = 'Group' and RG.GroupID = O.ResponsibleObjectID
					inner join ResponsibilityDetail RD on RD.ResponsibilityID = O.ID
					inner join reporting.Global_Resource R on R.ResourceID = coalesce(RG.ResourceID, O.ResponsibleObjectID)
			where	O.ResponsibilityTypeID = @id
			group by O.ResponsibilityTypeID, coalesce(RG.ResourceID, O.ResponsibleObjectID), R.FirstName,R.LastName", new { id = id }).ToList();
            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{resourceID:int}/{responsibilityTypeID:int}/ResponsibilitiesByResource")]
        public JsonNetResult GetResponsibilitiesByResource(int resourceID, int responsibilityTypeID)
        {
            var query = Company.Query<dynamic>(@"select	* 
from	(
		select	distinct 
                RD.ObjectType,
				RD.ObjectID,
				RD.ObjectName,
				RD.ObjectTypeName,
				RD.RedFlagged,
				RD.ObjectUrl,
				RD.ContextItems,
				RD.CurrentScore,
				COALESCE(RG.ResourceID, RD.ResponsibleObjectID) as OwnerID 
		from	ResponsibilityDetail RD
				left join ResourceGroup RG on RD.ResponsibleObjectType = 'Group' and RG.GroupID = RD.ResponsibleObjectID and Visible = 1
		where	RD.ResponsibilityTypeID = @r
		) O where O.OwnerID = @id order by O.ObjectTypeName, O.ObjectName", new { id = resourceID, r = responsibilityTypeID }).ToList();

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{type}/{id:int}/ScoreHistoryByObject")]
        public JsonNetResult GetScoreHistoryByObject(SystemObjects type, int id)
        {
            var query = Company.Query<dynamic>(@"EXEC GetScoreHistoryByObject @type, @id", new { type = type.ToString(), id = id });
            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{type}/{id:int}/AverageScoreByObjectType")]
        public JsonNetResult GetAverageScoreByObjectType(SystemObjects type, int id)
        {
            var query = Company.Query<dynamic>(@"EXEC GetAverageScoreByObjectType @type, @id", new { type = type.ToString(), id = id }).SingleOrDefault();
            return new JsonNetResult{ Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{type}/{id:int}/PointBreakdownByObject")]
        public JsonNetResult GetPointBreakdownByObject(SystemObjects type, int id)
        {
            var query = Company.Query<dynamic>(@"select T.Name, R.Score as MaxScore, coalesce(S.Score, 0) as Score
from	StatisticType T
inner join cache.ObjectDetails D on D.[Object] = @type and D.ObjectID = @id
inner join StatisticTypeRelation R	on R.StatisticTypeID = T.ID and R.ObjectType = D.[ObjectType]  and R.ObjectID = D.ObjectTypeID and T.PartOfScore = 1
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
                                        and C.ParentID is null", new { type = type, id = id });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult RelatedArtifacts(int artifactID)
        {
            var query = Company.Query<dynamic>(@"select TA.*, 
dbo.GenerateObjectUrl('Artifact', TA.ArtifactTypeID, TA.ID) as Url
from	RelatedArtifact SR
		inner join RelatedArtifact TR on TR.GroupID = SR.GroupID and SR.ArtifactID = @artifactID
		inner join Artifact TA on TR.ArtifactID = TA.ID and TA.ID <> SR.ArtifactID", new { artifactID = artifactID });

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

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