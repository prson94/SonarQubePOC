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
        Authorize
    ]
    public class BrowserController : BaseV2ApiController
    {
        public BrowserController(ICommunityContext community, ICompanyContext company) : base(community, company)
        {
        }

        #region Internal Classes For Endpoint Below

        internal class RawResultList1
        {
            public int Hop { get; set; }
            public long ID { get; set; }
        }

        internal class RawResultList2
        {
            public int Hop { get; set; }
            public long ID { get; set; }
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
            public bool Reveal { get; set; }
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
                    var child = new AssetBrowserLineageApiItemModel { ID = h.ID, key = h.Key, assetUid = h.AssetUid, displayValue = h.DisplayValue };

                    recurse(hierarchies, child);

                    if (current.items == null)
                    {
                        current.items = new List<AssetBrowserLineageApiItemModel>();
                    }
                    current.items.Add(child);
                }
            }
        }

        /// <summary>
        /// Gets lineage for the specified asset.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="assetUid">The uid of the asset that we are getting lineage for.</param>
        /// <param name="postModel"></param>
        /// <returns>An HTTP status code and message.</returns>
        [
            Route("{assetUid:Guid}"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(GetAssetLineagePostModel), typeof(GetAssetLineagePostModelExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserLineageApiResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetLineage(Guid assetUid, GetAssetLineagePostModel postModel)
        {
            try
            {
                var reader = await Company.QueryMultipleAsync(@"exec graph.GetLineageByAsset @assetUid, @startFromAssets, @direction, @hops", new { 
                    assetUid, 
                    startFromAssets = postModel.StartFromAssetsJson, 
                    direction = (int)postModel.Direction, 
                    hops = (postModel.Hops > 0) ? postModel.Hops : 1 
                }, timeout: 10);

                var hops = reader.Read<RawResultList1>().ToList();
                var hierarchies = reader.Read<RawResultList2>().OrderBy(i => i.Hop).ThenBy(i => i.ID).ThenBy(i => i.HierarchyLevel).ToList();
                var relationships = reader.Read<RawResultList3>().OrderBy(i => i.Hop).ToList();

                var model = new AssetBrowserLineageApiResponseModel {
                    focalAssetUid = assetUid
                };

                foreach (var h in hierarchies.Where(i => string.IsNullOrEmpty(i.ParentKey)))
                {
                    var current = new AssetBrowserLineageApiTopItemModel { ID = h.ID, key = h.Key, assetUid = h.AssetUid, backColor = h.Back, foreColor = "", displayValue = h.DisplayValue };
                    recurse(hierarchies, current);
                    model.assets.Add(current);
                }

                //int currentHop = 1;
                //int currentHierarchyLevel = 0;
                //IAssetBrowserLineageApiItemModel current = null;
                //hierarchies.ForEach(h =>
                //{
                //    if (h.Hop != currentHop || (h.Hop == currentHop && current?.ID != h.ID))
                //    {
                //        currentHop = h.Hop;
                //        current = null;
                //    }

                //    if (current == null)
                //    {
                //        current = new AssetBrowserLineageApiTopItemModel { ID = h.ID, key = h.Key, assetUid = h.AssetUid, backColor = h.Back, foreColor = "", displayValue = h.DisplayValue };
                //        model.assets.Add((AssetBrowserLineageApiTopItemModel)current);
                //    }
                //    else
                //    {
                //        if (current.assetUid != h.AssetUid)
                //        {
                //            if (h.HierarchyLevel != currentHierarchyLevel)
                //            {
                //                current.items = new List<AssetBrowserLineageApiItemModel>();
                //                var newCurrent = new AssetBrowserLineageApiItemModel { ID = h.ID, key = h.Key, assetUid = h.AssetUid, displayValue = h.DisplayValue };
                //                current.items.Add(newCurrent);

                //                current = newCurrent;
                //            }
                //            else
                //            { 
                            
                //            }
                //        }
                //    }
                //});

                model.intersects = relationships.Select(r => new AssetBrowserLineageApiRelationshipModel { 
                    backColor = "", 
                    foreColor = "", 
                    intersectUid = r.Uid, 
                    objectUid = r.objectUid, 
                    objectKey = r.objectKey,
                    predicate = r.predicate, 
                    predicateUid = r.predicateUid, 
                    predicateType = r.predicateType,
                    subjectUid = r.subjectUid ,
                    subjectKey = r.subjectKey
                }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        private void update(AssetBrowserLineageApiResponseModel root, AssetBrowserLineageApiItemModel current)
        { 
        
        }
    }
}
