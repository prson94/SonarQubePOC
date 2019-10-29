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
                    
                    var child = new AssetBrowserLineageApiItemModel { hop = h.Hop, key = h.Key, assetUid = h.AssetUid, displayValue = h.DisplayValue, reveal = h.Reveal, relationCounts = relationCounts };

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

                var current = new AssetBrowserLineageApiTopItemModel { hop = h.Hop, key = h.Key, assetUid = h.AssetUid, backColor = h.Back, foreColor = "", displayValue = h.DisplayValue, reveal = h.Reveal, relationCounts = relationCounts };
                recurse(hierarchies, current);
                model.assets.Add(current);
            }

            model.intersects = relationships.Select(r => new AssetBrowserLineageApiRelationshipModel
            {
                backColor = "",
                foreColor = "",
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
        /// Gets lineage for the specified assets.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="postModel"></param>
        /// <returns>An HTTP status code and message.</returns>
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
        public async Task<HttpResponseMessage> GetAssetLineage(GetAssetLineagePostModel postModel)
        {
            try
            {
                var reader = await Company.QueryMultipleAsync(@"exec graph.GetLineageByAsset @assets, @IsReveal, @StartHop, @direction, @hops", new { 
                    assets = postModel.AssetUids.AsTableValuedParameter<Guid>(
                        "dbo.UidTable", 
                        new List<string>() {"Uid"}
                        ), 
                    postModel.IsReveal,
                    postModel.StartHop,
                    direction = (int)postModel.Direction, 
                    hops = (postModel.Hops > 0) ? postModel.Hops : 1 
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
        /// Gets impact relationships for the specified assets.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="postModel"></param>
        /// <returns>An HTTP status code and message.</returns>
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
        public async Task<HttpResponseMessage> GetAssetImpacts(GetAssetImpactsPostModel postModel)
        {
            try
            {
                var reader = await Company.QueryMultipleAsync(@"exec graph.GetImpactRelationshipsByAssets @assets, @StartHop", new
                {
                    assets = postModel.AssetUids.AsTableValuedParameter<Guid>(
                        "dbo.UidTable",
                        new List<string>() { "Uid" }
                        ),
                    postModel.StartHop
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
        /// Gets information regarding a specific diagram asset, beOwner it an asset or a relationship.
        /// </summary>
        /// <param name="uid">The uid of the asset that we are getting lineage for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            Route("diagramasset/{uid:Guid}"),
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
			select	F.FriendlyName as Name,
					V.FormattedValue as Value,
					F.Type
			from	utility.FieldValue V
					inner join FieldType F on F.ID = V.FieldTypeID and F.[Type] not in @CalculatedFieldTypes
			where	AssetID = A.ID
					and F.IsDisplayable = 1
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

                var reader = await Company.QueryAsync<string>(sql, new { uid, this.CalculatedFieldTypes }, timeout: 10);
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
