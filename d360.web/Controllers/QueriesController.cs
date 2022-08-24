using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
	[RoutePrefix("queries"), Authorize, AiHandleError]
	public class QueriesController : BaseController
	{
		#region DI

		public QueriesController(CoreComponentSet set) : base(set)
		{ }

		#endregion

		[Route("FollowingByResourceByType"), NonNullableParameters]
		public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
		{
			var query = Company.Query<dynamic>(@"select A.Object ObjectType, f.AssetID ObjectID, f.Name, f.ID, f.Url, f.CurrentScore, OpenEventCount
												from FollowDetail f
												inner join Asset a on f.Assetid = a.id
												where f.ResourceID = @r and f.AssetTypeID = @i and f.AssetId is not null", new { r = resourceID, i = id });

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
			long assetid = 0;

			if (Company.Assets.Any(x => x.Object == type && x.ObjectID == id))
			{
				assetid = Company.Assets.Where(x => x.Object == type && x.ObjectID == id).SingleOrDefault().ID;
			}

			var query = Company.Query<dynamic>(@"
												select 'followers' as Suffix, count(1) as [Count], 'Followers' as Name
												from	Follow F
												where	F.AssetID = @id
												union all
												select 'comments' as Suffix, count(1) as [Count], 'Comments' as Name
												from	Comment C
														inner join CommentRelation R	
														on R.CommentID = C.ID 
														and C.ParentID is null
														and R.AssetID  = @id",
												new {assetid});

			return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
		}
	}
}
