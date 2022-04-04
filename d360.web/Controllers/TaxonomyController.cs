using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using d360.core.enums;
using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
	[RoutePrefix("internal/taxonomy"), Authorize]
	public class TaxonomyController : BaseController
	{
		#region DI

		public TaxonomyController(ICoreComponentSet set)
			: base(set)
		{
		}

		#endregion

		#region JSON

		[HttpGet, Route("ModelHierarchy"), NonNullableParameters]
		public JsonNetResult ModelHierarchy(int id)
		{
			var models = Company.Query<dynamic>($@"
					select	A.ObjectID as ID,
							A.[Uid],
							A.DisplayValue,
							A.DisplayValue as TextPath,
							P.SubjectID as ParentID,
							A.ID as AssetID,        
							CASE WHEN EXISTS (select 1 from report where [objecttype] = 'Taxonomy' and [objectid] = A.TypeID)    
								THEN 1  
								ELSE 0 
							END AS 'HasDashboards',
							case 
									when Work.[Count] > 0 then cast(1 as bit)
									else cast(0 as bit)
								end as HasWorkflow
					from	AssetDetail A
							inner join AssetType AT on AT.ID = A.AssetTypeID
							outer apply (
										select	I.SubjectID
										from	[Intersect] I
												inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
												inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
										) P
							cross apply (
										select	count(1) as [Count]
										from	workflow.EventRegistration WER
												inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 
										where	WER.Object = AT.Object
												and WER.ObjectID = AT.ObjectID
										) Work
		
					where   A.Type = 'TaxonomyType' and A.TypeID = @id AND A.[State] = 1 order by A.DisplayValue", new { id });

			return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
		}

		[HttpGet, Route("ModelHierarchyDetailed"), NonNullableParameters]
		public JsonNetResult ModelHierarchyDetailed(int id, bool stripHtml = false)
		{
			var fields = getFieldTypesByObjectType("TaxonomyType", id, true);

			// get the dynamic fields set as listable for this taxonomy
			getDynamicFieldJoinStatements(id, "Taxonomy", out string joins, out string columns, false, false, true, fields, "A.ObjectID");

			List<string> orderFields = fields.Where(x => x.SortOrder > 0 && x.IsListable == true)
				.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
				.Select(f => "Field" + f.ID.ToString())
				.ToList();
			orderFields.Add("DisplayValue");
			string orderBySql = "order by " + string.Join(", ", orderFields.ToArray());

			var editRightsColumnStatement = "cast(1 as bit) as P_CanEdit, cast(1 as bit) as P_CanDelete,";
			var editRightsJoinStatement = "";

			if (!Company.CurrentResourceIsAdmin)
			{
				editRightsColumnStatement = " cast(IIF(S_E.[Count] = 0, 0, 1) as bit) as P_CanEdit, cast(IIF(S_D.[Count] = 0, 0, 1) as bit) as P_CanDelete, ";
				editRightsJoinStatement = $@"cross apply (select count(1) as [Count] from ResponsibilityDetail where ResourceID = @r and ( (AssetID = A.ID) or (AssetTypeID = A.AssetTypeID and AssetID = 0) ) and PermissionsBitMask & {(int)Permission.EditAsset} = {(int)Permission.EditAsset}) as S_E 
											cross apply (select count(1) as [Count] from ResponsibilityDetail where ResourceID = @r and ( (AssetID = A.ID) or (AssetTypeID = A.AssetTypeID and AssetID = 0) ) and PermissionsBitMask & {(int)Permission.DeleteAsset} = {(int)Permission.DeleteAsset}) as S_D ";
			}

			var sql = $@"
						select	A.ObjectID as ID, 
								A.[Uid],
								P.SubjectID as ParentID, 
								P.uid as ParentUid,
								A.AssetTypeUid as AssetTypeUid,
								A.TypeID as TaxonomyTypeID,
								{editRightsColumnStatement}
								A.ID as AssetID,  
								{columns} 
								D.DisplayValue
						from	AssetWithType A        
								{joins} 
								inner join dbo.AssetDisplayValue D on A.ID = D.AssetID
								{editRightsJoinStatement}
								outer apply (
											select	top 1 I.SubjectID, A.uid
											from	[Intersect] I
													inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
													inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
													inner join Asset A on A.ObjectId = I.SubjectID and A.Object = I.Subject
											) P
						where   A.Type = 'TaxonomyType' 
								and A.TypeID = @id 
								and A.[State] = 1 
								and A.ID not in ({GetNoReadSqlStatement()}) 
								and A.AssetTypeID not in ({GetAssetTypeNoReadSqlStatement()})
						{orderBySql} ";

			var models = Company.Query<dynamic>(sql, new { id, r = Company.CurrentResourceID });

			return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
		}

		#endregion
	}
}
