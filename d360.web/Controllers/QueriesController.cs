using d360.core;
using d360.model;
using d360.web.Models.Attributes;
using Dapper;
using System.Collections.Generic;
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

        public QueriesController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
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
            DynamicParameters dbArgs = new DynamicParameters();
            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();
            var fieldTypes = Company.FieldTypes.Where(f => f.Object == "ResourceType" && f.ObjectID == 1 && f.IsListable).OrderBy(f => f.ID).ToList();
            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);
            string fieldColumnsSql = "";
            if (fieldColumns.Count > 0)
            {
                fieldColumnsSql = "," + string.Join(",\n", fieldColumns);
            }

            string fieldJoinsSql = "";
            if (fieldJoins.Count > 0)
            {
                fieldJoinsSql = "\n outer apply(select object, objectid from Asset A1 where A1.Object = 'Resource' and A1.ObjectID = gr.ResourceID) A " + fieldJoinsSql;
                fieldJoinsSql += string.Join("\n", fieldJoins);
            }

            var sql = $@"
drop table if exists #respdata;

select		OC.ResourceID,
			R.FirstName,
			R.LastName,
			OC.ResponsibilityTypeID,
			sum(OC.[Count] * OC.AssetCount) as OwnedItemCount
into #respdata
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
			OC.ResponsibilityTypeID;

select gr.ResourceID,
       gr.FirstName,
	   gr.LastName,
	   gr.ResponsibilityTypeID,
	   gr.OwnedItemCount
       {fieldColumnsSql}
from #respdata gr
{fieldJoinsSql}
order by	gr.LastName, gr.FirstName;";

            var query = Company.Query<dynamic>(sql, new { id }).ToList();
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