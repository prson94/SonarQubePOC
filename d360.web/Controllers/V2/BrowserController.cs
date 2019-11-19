using d360.core.entities;
using d360.model;
using Microsoft.Web.Http;
using System;
using System.Web.Http;
using d360.core;
using System.Linq;
using System.Data.SqlClient;
using d360.core.enums;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using d360.web.Filters;
using Swashbuckle.Swagger.Annotations;
using d360.web.Models;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service supports all asset browser functionality for Lineage version 3.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/browser"),
        Authorize,
        StringEnumController
    ]
    public class BrowserController : BaseV2ApiController
    {
        public BrowserController(ICommunityContext community, ICompanyContext company) : base(community, company)
        {
        }

        #region Internal Classes For GetAssetLineage Endpoint

        internal class RawResultList2
        {
            public int Hop { get; set; }
            public string Key { get; set; }
            public string ParentKey { get; set; }
            public string Back { get; set; }
            public string Fore { get; set; }
            public string Icon { get; set; }
            public int HierarchyLevel { get; set; }
            public int AssetTypeID { get; set; }
            public long AssetID { get; set; }
            public Guid AssetUid { get; set; }
            public string DisplayValue { get; set; }
            public AssetTypeClass Class { get; set; }
            public string AssetTypeName { get; set; }
            public GetAssetLineagePostModelDirection Reveal { get; set; }
            public string RelationCounts { get; set; }
        }

        internal class RawResultList3
        {
            public int Hop { get; set; }
            public Guid Uid { get; set; }
            public Guid subjectUid { get; set; }
            public string subjectKey { get; set; }
            public long subjectId { get; set; }
            public Guid objectUid { get; set; }
            public string objectKey { get; set; }
            public long objectId { get; set; }
            public Guid predicateUid { get; set; }
            public string predicate { get; set; }
            public PredicateType predicateType { get; set; }
        }

        #endregion

        private void recurse(List<RawResultList2> hierarchies, IAssetBrowserLineageApiItemModel current)
        {
            if (hierarchies.Any(h => h.ParentKey == current.key))
            {
                foreach (var h in hierarchies.Where(h => h.ParentKey == current.key))
                {
                    // For badging in browser.
                    var relationCounts = new List<AssetBrowserLineageApiItemRelationCountModel>();
                    if (!string.IsNullOrEmpty(h.RelationCounts))
                    {
                        relationCounts = JsonConvert.DeserializeObject<List<AssetBrowserLineageApiItemRelationCountModel>>(h.RelationCounts);
                    }
                    
                    var child = new AssetBrowserLineageApiItemModel { hop = h.Hop, key = h.Key, assetUid = h.AssetUid, icon = h.Icon, @class = h.Class, displayValue = h.DisplayValue, reveal = h.Reveal, relationCounts = relationCounts };

                    recurse(hierarchies, child);

                    if (current.items == null)
                    {
                        current.items = new List<AssetBrowserLineageApiItemModel>();
                    }
                    current.items.Add(child);
                }
            }
        }

        private AssetBrowserLineageApiResponseModel buildResponseModel(List<RawResultList2> hierarchies, List<RawResultList3> relationships)
        {
            var model = new AssetBrowserLineageApiResponseModel();

            foreach (var h in hierarchies.Where(i => string.IsNullOrEmpty(i.ParentKey)))
            {
                // For badging in browser.
                var relationCounts = new List<AssetBrowserLineageApiItemRelationCountModel>();
                if (!string.IsNullOrEmpty(h.RelationCounts))
                {
                    relationCounts = JsonConvert.DeserializeObject<List<AssetBrowserLineageApiItemRelationCountModel>>(h.RelationCounts);
                }

                var current = new AssetBrowserLineageApiTopItemModel { hop = h.Hop, key = h.Key, assetUid = h.AssetUid, backColor = h.Back, foreColor = "", icon = h.Icon, @class = h.Class, displayValue = h.DisplayValue, reveal = h.Reveal, relationCounts = relationCounts };
                recurse(hierarchies, current);
                model.assets.Add(current);
            }

            model.intersects = relationships.Select(r => new AssetBrowserLineageApiRelationshipModel
            {
                backColor = "",
                foreColor = "",
                icon = "",
                intersectUid = r.Uid,
                objectUid = r.objectUid,
                objectKey = r.objectKey,
                predicate = r.predicate,
                predicateUid = r.predicateUid,
                predicateType = r.predicateType,
                subjectUid = r.subjectUid,
                subjectKey = r.subjectKey
            }).ToList();

            return model;
        }

        /// <summary>
        /// Retrieves lineage relationships for the specified set of assets.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="criteria">
        /// An object containing:
        /// 1. AssetUids: A set of asset Uids you want to retrieve lineage for. 
        /// 1. IsReveal: A true/false value indicating whether this call is from clicking a Reveal button, or is from an initial call to get starting lineage.
        /// 2. StartHop: A starting point, which will be used to generate unique key values that are used in the Asset Browser UI.
        /// 3. Direction: An enumeration value (Backward, Both, Forward) indicating the direction you want to traverse when getting relationships. Backward is upstream, Forward is downstream.
        /// 4. Hops: The number of hops, or traversals, you want to pull. The more hops, the slower the API response.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route(""),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(GetAssetLineagePostModel), typeof(GetAssetLineagePostModelExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserLineageApiResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetLineage(GetAssetLineagePostModel criteria)
        {
            try
            {
                var reader = await Company.QueryMultipleAsync(@"exec graph.GetLineageByAsset @assets, @IsReveal, @StartHop, @direction, @hops", new { 
                    assets = criteria.AssetUids.AsTableValuedParameter<Guid>(
                        "dbo.UidTable", 
                        new List<string>() {"Uid"}
                        ), 
                    criteria.IsReveal,
                    criteria.StartHop,
                    direction = (int)criteria.Direction, 
                    hops = (criteria.Hops > 0) ? criteria.Hops : 1 
                }, timeout: 60);

                var hierarchies = reader.Read<RawResultList2>().ToList();
                var relationships = reader.Read<RawResultList3>().ToList();

                return Request.CreateResponse(HttpStatusCode.OK, buildResponseModel(hierarchies, relationships));
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        /// <summary>
        /// Retrieves impact relationships for the specified set of assets.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="criteria">
        /// An object containing:
        /// 1. Assets: A set of assets (Uid and unique Key value) you want to retrieve impacts for. 
        /// 2. PredicateUid: The Uid of the predicate you are getting impacted relationships for.
        /// 3. StartHop: A starting point, which will be used to generate unique key values that are used in the Asset Browser UI.
        /// </param>
        /// <returns>An object containing impacts results, as well as an HTTP status code and message.</returns>
        [
            Route("impacts"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(GetAssetImpactsPostModel), typeof(GetAssetImpactsPostModelExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserLineageApiResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetImpacts(GetAssetImpactsPostModel criteria)
        {
            try
            {
                var reader = await Company.QueryMultipleAsync(@"exec graph.GetImpactRelationshipsByAssets @assets, @PredicateUid, @StartHop", new
                {
                    assets = criteria.Assets.AsTableValuedParameter(
                        "dbo.AssetBrowserImpactTable",
                        new List<string>() { "Key", "Uid" }
                        ),
                    criteria.PredicateUid,
                    criteria.StartHop
                }, timeout: 60);

                var hierarchies = reader.Read<RawResultList2>().OrderBy(i => i.Hop).ThenBy(i => i.HierarchyLevel).ToList();
                var relationships = reader.Read<RawResultList3>().OrderBy(i => i.Hop).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, buildResponseModel(hierarchies, relationships));
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }


        #region Internal Classes For GetDiagramAsset Endpoint

        internal class AssetBrowserDiagramAsset
        {
            public string TypeName { get; set; }
            public AssetTypeClass AssetTypeClass { get; set; }
            public string AssetTypeClassDisplayName { get { return AssetTypeClass.GetDisplayName(); } }
            public Guid Uid { get; set; }
            public string DisplayValue { get; set; }
            public string Path { get; set; }
            public string Url { get; set; }
            public List<AssetBrowserDiagramAssetField> Fields { get; set; } = new List<AssetBrowserDiagramAssetField>();
            public List<AssetBrowserDiagramAssetScore> Scores { get; set; } = new List<AssetBrowserDiagramAssetScore>();
            public List<AssetBrowserDiagramAssetOwner> Owners { get; set; } = new List<AssetBrowserDiagramAssetOwner>();
        }

        internal class AssetBrowserDiagramAssetField
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Values { get; set; }
            public string Type { get; set; }
        }

        internal class AssetBrowserDiagramAssetScore
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        internal class AssetBrowserDiagramAssetOwner
        {
            public int ResponsibilityTypeID { get; set; }
            public string ResponsibilityTypeName { get; set; }
            public string Icon { get; set; }
            public int ResourceID { get; set; }
            public string ResourceName { get; set; }
            public string SecurityAssetName { get; set; }
            public string Context { get; set; }
        }

        #endregion

        /// <summary>
        /// Gets detailed field information regarding a specific asset that a user selects from the Asset Browser UI.
        /// </summary>
        /// <param name="uid">The uid of the asset that we are getting field information for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            Route("diagramasset/{uid:Guid}"),
            ApiExplorerSettings(IgnoreApi = true),
            HttpGet,
            MapToApiVersion("2.0"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the GET request.", typeof(AssetBrowserDiagramAsset)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetDiagramAsset(Guid uid)
        {
            try
            {
                var ignoredFields = this.CalculatedFieldTypes;
                ignoredFields.Add(DataType.Tag.ToString());
                ignoredFields.Add(DataType.Relationship.ToString());
                ignoredFields.Add(DataType.FieldFromRelationship.ToString());
                var sql = @"
select	A.TypeName,
		A.Uid,
        A.AssetTypeClass,
		A.DisplayValue,
		(
			select	string_agg(doc.c.value('.', 'nvarchar(250)'), ' > ')
			from	graph.AssetNode AN
					cross apply AN.Segments.nodes('/path/segment') doc(c)
			where	AN.ID = A.ID
		) as [Path],
		dbo.GenerateAssetUrl(A.ID) as Url,
		(
			select  *
            from    (
                    select	F.ColumnOrder,
							F.FriendlyName as Name,
					        V.FormattedValue as Value,
                            '[]' as [Values],
					        F.Type
			        from	utility.FieldValue V
					        inner join FieldType F on F.ID = V.FieldTypeID and F.[Type] not in @ignoredFields
			        where	AssetID = A.ID
					        and F.IsDisplayable = 1
                    union all
                    select	F.ColumnOrder,
							F.FriendlyName as Name,
					        null as Value,
                            TV.[Values],
					        F.Type
			        from	FieldType F 
                            inner join Asset TA on TA.ID = A.ID and F.AssetTypeID = TA.AssetTypeID and F.[Type] = 'Tag'
                            cross apply (
                                select	( 
										select  T.Value,
												'tag' as TooltipType,
												T.ID as TooltipID,
												T.CreatedBy,
												'Preview' as TooltipContext,
												'' as TooltipUrl,
												T.[uid]
										from    AssetTag TJ
												inner join Tag T on T.ID = TJ.TagID and TJ.AssetID = TA.ID
										for json path
										) as [Values]
                            ) TV
                    ) F
			order by F.ColumnOrder
			for json path
		) as Fields,
		(
			select	'Governance Score' as Name,
					cast([Value]*100 as int) as Value
			from	metrics.Score
			where	AssetUid = A.Uid
			for json path
		) as Scores,
		(
			select	ResponsibilityTypeID,
					ResponsibilityTypeName,
					case SecurityAsset
						when 'G' then 'fa-users'
						else 'fa-user'
					end as Icon,
					ResourceID,
					ResourceName,
					SecurityAssetName,
					Context
			from	ResponsibilityDetail
			where	IsVisible = 1 
					and AssetID = A.ID
			for json path
		) as Owners
from	AssetDetail A
where	A.Uid = @uid
for json path, WITHOUT_ARRAY_WRAPPER";

                var reader = await Company.QueryAsync<string>(sql, new { uid, ignoredFields }, timeout: 10);
                var json = string.Join("",reader);

                var model = JsonConvert.DeserializeObject<AssetBrowserDiagramAsset>(json);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }
    }
}
