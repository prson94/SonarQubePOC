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
using System.Security.Cryptography;
using System.Text;
using d360.core.entities.Views;

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

        internal class HopNodeResult
        {
            public Guid assetUid { get; set; }
            public long assetID { get; set; }
            public string key { get; set; }
            public long parentID { get; set; }
            public string parentKey { get; set; }
            public string back { get; set; }
            public string fore { get; set; }
            public string icon { get; set; }
            public int assetTypeID { get; set; }
            public Guid assetTypeUid { get; set; }
            public AssetTypeClass @class { get; set; }
            public string displayValue { get; set; }
            public AssetBrowserApiHopDirection reveal { get; set; }
            public string ownerCounts { get; set; }
            public string relationCounts { get; set; }
            public bool useAsTransformation { get; set; }
            public bool isSubjectInTransformation { get; set; }
            public bool isLeaf { get; set; }

        }

        internal class HopLinkResult
        {
            public Guid uid { get; set; }
            public string subjectKey { get; set; }
            public string objectKey { get; set; }
            public int predicateId { get; set; }
            public Guid predicateUid { get; set; }
            public string predicate { get; set; }
            public PredicateType predicateType { get; set; }
        }

        internal class HopModel 
        {
            public List<HopNodeResult> nodes { get; set; }
            public List<HopLinkResult> links { get; set; }
        }

        #endregion

        List<T> parseArrayCount<T>(string json)
        {
            var arr = new List<T>();
            arr = JsonConvert.DeserializeObject<List<T>>(json ?? "[]");
            return arr;
        }

        private void recurse(AssetBrowserAssetsModel model, List<HopNodeResult> hierarchies, AssetBrowserAssetModel current, int multiplier)
        {
            foreach (var h in hierarchies.Where(h => h.parentKey == current.key && h.key != current.key))
            {
                var child = new AssetBrowserAssetModel
                {
                    key = h.key,
                    parentKey = h.parentKey,
                    assetUid = h.assetUid,
                    assetTypeId = h.assetTypeID,
                    assetTypeUid = h.assetTypeUid,
                    backAmount = ((multiplier <= 4) ? multiplier : 4) * .2,
                    backColor = h.back,
                    foreAmount = 0,
                    foreColor = h.fore,
                    icon = h.icon,
                    @class = h.@class,
                    displayValue = h.displayValue,
                    reveal = h.reveal,
                    ownerCounts = parseArrayCount<AssetBrowserOwnerCountModel>(h.ownerCounts),
                    relationCounts = parseArrayCount<AssetBrowserAssetRelationCountModel>(h.relationCounts),
                    useAsTransformation = h.useAsTransformation,
                    isSubjectInTransformation = h.isSubjectInTransformation
                };

                recurse(model, hierarchies, child, multiplier+1);

                if (current.items == null)
                {
                    current.items = new List<AssetBrowserAssetModel>();
                }
                current.items.Add(child);
                //model.assets.Add(child);
            }
        }

        private AssetBrowserAssetsModel buildResponseModel(List<HopNodeResult> hierarchies, List<HopLinkResult> relationships, int multiplier)
        {
            var model = new AssetBrowserAssetsModel();

            foreach (var h in hierarchies.Where(i => string.IsNullOrEmpty(i.parentKey)))
            {
                var current = new AssetBrowserAssetModel { 
                    key = h.key, 
                    assetUid = h.assetUid, 
                    assetTypeId = h.assetTypeID, 
                    assetTypeUid = h.assetTypeUid,
                    backAmount = ((multiplier <= 4) ? multiplier : 4) * .2,
                    backColor = h.back, 
                    foreAmount = 0,
                    foreColor = h.fore, 
                    icon = h.icon, 
                    @class = h.@class, 
                    displayValue = h.displayValue, 
                    reveal = h.reveal,
                    ownerCounts = parseArrayCount<AssetBrowserOwnerCountModel>(h.ownerCounts),
                    relationCounts = parseArrayCount<AssetBrowserAssetRelationCountModel>(h.relationCounts),
                    useAsTransformation = h.useAsTransformation, 
                    isSubjectInTransformation = h.isSubjectInTransformation 
                };
                recurse(model, hierarchies, current, multiplier + 1);
                model.assets.Add(current);
            }

            model.assetRelations = relationships.Select(r => new AssetBrowserAssetRelationModel
            {
                backColor = "",
                foreColor = "",
                icon = "",
                intersectUid = r.uid,
                objectUid = Guid.NewGuid(),
                objectKey = r.objectKey,
                predicate = r.predicate,
                predicateId = r.predicateId,
                predicateUid = r.predicateUid,
                predicateType = r.predicateType,
                subjectUid = Guid.NewGuid(),
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
        /// 1. Assets: A set of asset you want to retrieve lineage for. 
        /// 2. IsReveal: A true/false value indicating whether this call is from clicking a Reveal button, or is from an initial call to get starting lineage.
        /// 3. Direction: An enumeration value (Backward, Both, Forward) indicating the direction you want to traverse when getting relationships. Backward is upstream, Forward is downstream.
        /// 4. Hops: The number of hops, or traversals, you want to pull. The more hops, the slower the API response.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route(""),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(AssetBrowserApiHopRequestModel), typeof(GetAssetLineagePostModelExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserAssetsModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetHop(AssetBrowserApiHopRequestModel criteria)
        {
            try
            {
                Func<string> generateSalt = delegate ()
                {
                    Random random = new Random();
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    return new string(Enumerable.Repeat(chars, 25).Select(s => s[random.Next(s.Length)]).ToArray());
                };
                
                var model = new HopModel {
                    links = new List<HopLinkResult>(),
                    nodes = new List<HopNodeResult>()
                };

                // Create initial salt value for Hop 0.
                string HopSalt = generateSalt();

                string hashKey(string input)
                {
                    string returnValue = "";

                    var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(input));
                    returnValue = string.Concat(hash.Select(b => b.ToString("x2")));
                    returnValue = returnValue.Substring(2, returnValue.Length - 3);

                    return returnValue;
                }

                async Task<HopModel> getHop(List<AssetBrowserApiHopAssetRequestModel> hopAssets, string direction)
                {
                    var hopModel = new HopModel();

                    var reader = await Company.QueryMultipleAsync(@"exec graph.GetHop @assets, @hopSalt, @direction, @predicateUid", new
                    {
                        assets = hopAssets.AsTableValuedParameter(
                            "dbo.AssetBrowserImpactTable",
                            new List<string>() { "Key", "Uid" }
                            ),
                        HopSalt,
                        direction,
                        predicateUid = criteria.PredicateUid
                    }, timeout: 60);

                    hopModel.nodes = reader.Read<HopNodeResult>().ToList();
                    hopModel.links = reader.Read<HopLinkResult>().ToList();

                    return hopModel;
                }

                // Check to see if keys are populated on incoming assets. If not, populate with auto-generated salt.
                criteria.Assets.ForEach(a =>
                {
                    if (string.IsNullOrEmpty(a.Key))
                    {
                        a.Key = hashKey($"{HopSalt}|{a.Uid}");
                    }
                });

                if (criteria.Direction == AssetBrowserApiHopDirection.None && criteria.PredicateUid.HasValue)
                {
                    // This is an IMPACT call.
                    model = await getHop(criteria.Assets, "I");
                }
                else 
                {
                    // This is a LINEAGE call.
                    if (criteria.IsInitial)
                    {
                        model = await getHop(criteria.Assets, "");
                    }

                    if (criteria.Hops <= 0 || criteria.Hops > 15)
                    {
                        criteria.Hops = 3;
                    }

                    var backwardAssets = criteria.Assets;
                    var forwardAssets = criteria.Assets;
                    for (int i = 0; i < criteria.Hops; i++)
                    {
                        HopSalt = generateSalt(); // We have multiple hops, so we should reset the salt after each hop.
                        var backModel = await getHop(backwardAssets, "B");
                        var forwardModel = await getHop(forwardAssets, "F");

                        backwardAssets = backModel.nodes.Where(n => n.isLeaf).Select(n => new AssetBrowserApiHopAssetRequestModel { Key = n.key, Uid = n.assetUid }).ToList();
                        forwardAssets = forwardModel.nodes.Where(n => n.isLeaf).Select(n => new AssetBrowserApiHopAssetRequestModel { Key = n.key, Uid = n.assetUid }).ToList();

                        model.links.AddRange(backModel.links);
                        model.links.AddRange(forwardModel.links);
                        model.nodes.AddRange(backModel.nodes);
                        model.nodes.AddRange(forwardModel.nodes);
                    }
                }
                
                return Request.CreateResponse(HttpStatusCode.OK, buildResponseModel(model.nodes, model.links, 0));
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        /// <summary>
        /// Retrieves owners for the specified set of assets.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="criteria">
        /// An object containing:
        /// 1. Assets: A set of asset you want to retrieve lineage for. 
        /// 2. IsReveal: A true/false value indicating whether this call is from clicking a Reveal button, or is from an initial call to get starting lineage.
        /// 3. Hops: A starting point, which will be used to generate unique key values that are used in the Asset Browser UI.
        /// 4. Direction: An enumeration value (Backward, Both, Forward) indicating the direction you want to traverse when getting relationships. Backward is upstream, Forward is downstream.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route("owners"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            //SwaggerRequestExample(typeof(AssetBrowserApiHopRequestModel), typeof(GetAssetLineagePostModelExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserOwnersModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetOwnerHop(AssetBrowserApiOwnerHopRequestModel criteria)
        {
            try
            {
                var model = new HopModel
                {
                    links = new List<HopLinkResult>(),
                    nodes = new List<HopNodeResult>()
                };

                string hashKey(string input)
                {
                    string returnValue = "";

                    var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(input));
                    returnValue = string.Concat(hash.Select(b => b.ToString("x2")));
                    returnValue = returnValue.Substring(2, returnValue.Length - 3);

                    return returnValue;
                }

                var owners = Company.Query<AssetBrowserOwnerModel>(@"
select	distinct
		A.Uid as assetUid,
		R.ResourceName as displayValue, 
		'fa-user' as icon,
		RE.Uid as resourceUid
from	ResponsibilityDetail R
		inner join Asset A on ( (A.ID = R.AssetID) OR (R.AssetID = 0 and A.AssetTypeID = R.AssetTypeID) ) and R.IsVisible = 1
		inner join reporting.Global_Resource RE on RE.ResourceID = R.ResourceID
where	A.Uid in @assetUids
        and R.ResponsibilityTypeID = @ResponsibilityTypeId
order by R.ResourceName", new { assetUids = criteria.Assets.Select(i => i.Uid).ToList(), criteria.ResponsibilityTypeId }).ToList();

                // Check to see if keys are populated on incoming assets. If not, populate with auto-generated salt.
                owners.ForEach(o =>
                {
                    o.key = hashKey($"{o.assetUid}|{o.resourceUid}");
                });

                var ownerRelations = from o in owners
                                     join a in criteria.Assets on o.assetUid equals a.Uid
                                     select new AssetBrowserOwnerRelationModel
                                     {
                                         assetKey = a.Key,
                                         assetUid = a.Uid,
                                         backColor = o.backColor,
                                         foreColor = o.foreColor,
                                         ownerKey = o.key,
                                         ownerUid = o.resourceUid
                                     };

                return Request.CreateResponse(HttpStatusCode.OK, new { owners, ownerRelations });
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
			select	    top 1 
                        'Governance Score' as Name,
					    cast([Value]*100 as int) as Value
			from	    metrics.Score
			where	    AssetUid = A.Uid
            order by    EffectiveDate desc
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

        #region Filter Classes

        internal class AssetBrowserAssetTypeFilterItem {
            public Guid Uid { get; set; }
            public int AssetTypeId { get; set; }
            public int ClassId { get; set; }
            public string Class { get { return ((AssetTypeClass)ClassId).GetDisplayName(); } }
            public string Name { get; set; }
            public string Path { get; set; }
        }
        internal class AssetBrowserPredicateFilterItem {
            public int Id { get; set; }
            public Guid Uid { get; set; }
            public int TypeId { get; set; }
            public string Type { get { return ((PredicateType)TypeId).GetDisplayName(); } }
            public string Name { get; set; }
            public string Inverse { get; set; }
        }
        
        #endregion

        /// <summary>
        /// Retrieves lists of filters to be used in the Asset Browser. Hidden from Swagger as this is an internal API.
        /// </summary>
        /// <returns>Lists of filters for the asset browser.</returns>
        [
            Route("filters"),
            ApiExplorerSettings(IgnoreApi = true),
            HttpGet,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetBrowserFilters()
        {
            try
            {
                #region

                var sql = @"
with H as	(
			select	O.Class,
					O.[uid],
					O.ID as AssetTypeID,
					O.Object,
					O.ObjectID,
					cast(O.Name as nvarchar(2500)) as [Path],
					cast(null as int) as ParentAssetTypeID,
					1 as [Level]
			from	AssetType O
					outer apply (
								select	I.ID 
								from	IntersectType I 
										inner join [Predicate] P on P.ID = I.PredicateID and P.[Type] = 3 and I.Object = O.Object and I.ObjectID = O.ObjectID
								) I
			where	I.ID is null
					and O.Class in (1,7,8)
			union all
			select	O.Class,
					O.[uid],
					O.ID as AssetTypeID,
					O.Object,
					O.ObjectID,
					cast(H.Path + ' > ' + O.Name as nvarchar(2500)) as [Path],
					H.AssetTypeID as ParentAssetTypeID,
					H.[Level]+1 as [Level]
			from	AssetType O
					inner join IntersectType I on I.Object = O.Object and I.ObjectID = O.ObjectID
					inner join H on H.Object = I.Subject and H.ObjectID = I.SubjectID
					inner join [Predicate] P on P.ID = I.PredicateID and P.[Type] = 3
			)

select	*
from	(
		select		[Uid], [Path], AssetTypeID, Class as ClassId, [Level]
		from		H 
		where		[Level] = 1
					or AssetTypeID in (
										select	A.ID
										from	AssetType A
												inner join	(
															select	I.Subject,
																	I.SubjectID
															from	AssetType O
																	inner join IntersectType I on I.Object = O.Object and I.ObjectID = O.ObjectID
																	inner join [Predicate] P on P.ID = I.PredicateID and P.[Type] in (3,4)
																	left join IntersectType SI on SI.Subject = O.Object and SI.SubjectID = O.ObjectID and SI.PredicateID = P.ID
															where	O.Class in (1,7,8)
																	and SI.ID is null
															) S on S.Subject = A.Object and S.SubjectID = A.ObjectID			
									)
		union
		select	O.[Uid],
				cast(O.Name as nvarchar(2500)) as [Path],
				O.ID as AssetTypeID,
				O.Class as ClassId,
				1 as [Level]
		from	AssetType O
		where	O.Class in (2,6)
		) H
order by	ClassId, [Path];

select	Id,
        [Uid],
		[Type] as TypeId,	
		[Name],
		[Inverse]
from	[Predicate]
where	[Type] in (6,7,9)
order by [Type], [Name]";
                
                #endregion

                var reader = await Company.QueryMultipleAsync(sql, timeout: 60);

                var assetTypes = reader.Read<AssetBrowserAssetTypeFilterItem>().ToList();
                var predicates = reader.Read<AssetBrowserPredicateFilterItem>().ToList();

                return Request.CreateResponse(HttpStatusCode.OK, new { AssetTypeOptions = assetTypes, PredicateOptions = predicates });
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }
    }
}
