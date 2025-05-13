using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.core.search;
using d360.extensions;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Web.Http;
using repositories;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
	/// <summary>
	/// This service houses all endpoints handling search in Govern.
	/// </summary>
	[
		ApiVersion("2.0"),
		RoutePrefix("api/v{version:apiVersion}/search"),
		Authorize,
		StringEnum,
		ApiExplorerSettings(IgnoreApi = false)
	]
	public class SearchController : BaseV2ApiController
	{
		private readonly IAssetRepository AssetRepository;
		private readonly IQueueSource Queue;
		private readonly TelemetryClient Telemetry;

		//Icons set based on main Nav item for category
		private readonly Dictionary<string, string> siteNavMap;

		ISearch Search;

		public SearchController(ICoreComponentSet set, ISearch search, IAssetRepository assetRepository,IQueueSource queue) : base(set)
		{
			AssetRepository = assetRepository;
			Queue = queue;
			Telemetry = new TelemetryClient();

			Search = search;

			siteNavMap = new Dictionary<string, string> {
				{ Label.AssetTypeClass_Business, "#Business" },
				{ Label.AssetTypeClass_Technical, "#Technical" },
				{ Label.AssetTypeClass_Model, "#Models" },
				{ Label.AssetTypeClass_Reference, "#Reference" },
				{ Label.AssetTypeClass_Rule, "#Data Quality" },
				{ Label.AssetTypeClass_Policy, "#Policy" },
				{ Label.AssetTypeClass_SemanticType, "#SemanticTypes" }
			};
		}

		string cleanPhrase(string phrase)
		{
			bool hasDoubleQuotes = (phrase.StartsWith("\"") && !phrase.EndsWith("\"")) 
				|| (phrase.EndsWith("\"") && !phrase.StartsWith("\""))
				|| (phrase.StartsWith("\"") && phrase.EndsWith("\""));

			phrase = phrase.Replace("\"", "");
			if (hasDoubleQuotes)
			{
				phrase = $"\"{phrase}\"";
			}

			return phrase;
		}

		void loadSearchResultUris(List<SearchResult> results)
		{
			results.ForEach(r =>
			{
				if (string.IsNullOrEmpty(r.Icon))
				{
					r.Icon = "fa-book";
				}

				if (r.Class == AssetTypeClass.User)
				{
					r.Url = $"/users/{r.Uid}";
					r.AbsoluteUrl = $"https://{SecurityContext.CompanyPrefix}.data3sixty.com/users/{r.Uid}";

				}
				else if (r.Class == AssetTypeClass.Group)
				{
					r.Url = $"/admin/groups";
					r.AbsoluteUrl = $"https://{SecurityContext.CompanyPrefix}.data3sixty.com/admin/groups";

				}
				else if (r.Class == AssetTypeClass.Diagram && r.Url != null)
				{
					r.AbsoluteUrl = $"https://{SecurityContext.CompanyPrefix}.data3sixty.com{r.Url}";
				}
				else if (r.Class != AssetTypeClass.Reference)
				{
					r.Url = $"/asset/{r.Uid}";
					r.AbsoluteUrl = $"https://{SecurityContext.CompanyPrefix}.data3sixty.com/asset/{r.Uid}";
				}
				else
				{
					r.Url = $"/reference/{r.AssetTypeUid}/items";
					r.AbsoluteUrl = $"https://{SecurityContext.CompanyPrefix}.data3sixty.com/reference/{r.AssetTypeUid}/items";
				}
			});
		}

		/// <summary>
		/// Simple phrase search. Returns up to 200 results
		/// </summary>
		/// <param name="phrase">Search term</param>
		/// <returns></returns>
		[
			HttpGet,
			Route(""),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of matching search items.", typeof(SearchModel))
		]
		public async Task<IHttpActionResult> GetSearchResults(string phrase)
		{
			if (!string.IsNullOrEmpty(phrase))
			{
				//var queryParams = Request.GetQueryNameValuePairs();
				//int pageNumber = queryParams.CheckForPageNumber();
				//int pageSize = queryParams.CheckForPageSize();
				//int offset = (pageNumber - 1) * pageSize;
				//int take = pageSize;

				//phrase = cleanPhrase(phrase);
				var response = await Search.ReadResultsAsync(phrase, false, true, false, false, null, null, 0, 200);
				loadSearchResultUris(response.Data.Results);

				return Ok(response.Data);				
			}

			return Ok();
		}

		/// <summary>
		/// Global Search
		/// </summary>
		/// <remarks>
		/// Perform a search for any assets matching the search term.
		/// 
		/// If the Aggregations parameter is specified, the search will also return aggregate results as is shown in the Filters tab. The parameter is a comma separated list. Currently only the value "category" is supported.
		/// 
		/// AggregationFilters are filters applied to values returned from the Aggregation query. Filters are supported for "Category" and "AssetType"
		/// 
		/// FieldFilters are filters like the advanced filter bar. Fields supported are
		/// *   Name
		/// *   Description
		/// *   Path
		/// *   Tags
		/// </remarks>
		/// <param name="queryRequest">Search Query Request</param>
		/// <returns></returns>
		[
			HttpPost,
			Route("results"),
			SwaggerConsumes("application/json"),
			SwaggerProduces("application/json", "application/octet-stream"),
			SwaggerResponse(HttpStatusCode.OK, "Search results matching the query.", typeof(SearchModel)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your search request is invalid"),
			ApiExplorerSettings(IgnoreApi = false)
		]
		public async Task<IHttpActionResult> ReadResultsAsync(QueryRequest queryRequest)
		{
			var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
			
			string isValid = ValidateQueryRequest(queryRequest);

			if (!string.IsNullOrEmpty(isValid))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidRequest, isValid);
			}

			HttpResponseMessage response;

			List<AssetTypeClass> classes = null;
			List<Guid> types = null;

			//queryRequest.Term = cleanPhrase(queryRequest.Term);

			// Parse Aggregate Filters by looking at raw incoming text.
			if (queryRequest.AggregationFilters.Count > 0)
			{
				var rawClasses = queryRequest.AggregationFilters.Where(f => f.Class.HasValue);
				if (rawClasses.Count() > 0)
				{
					var classList = AssetTypeClass.BusinessAsset.GetAsList();
					foreach (var rawClass in rawClasses)
					{
						var assetTypeClass = classList.FirstOrDefault(cl => cl.ID == rawClass.Class);
						if (assetTypeClass != null)
						{
							if (classes == null) classes = new List<AssetTypeClass>();
							classes.Add(assetTypeClass.ID);
						}
					}
				}

				types = queryRequest.AggregationFilters
					.Where(f => f.Uid.HasValue && f.Uid != Guid.Empty)
					.Select(f => f.Uid.Value)
					.ToList();
			}

			var dbResponse = await Search.ReadResultsAsync(
				queryRequest.Term, 
				true, true, true, 
				queryRequest.IncludeAggregations, 
				classes, 
				types, 
				queryRequest.From, queryRequest.Size
				);
			
			loadSearchResultUris(dbResponse.Data.Results);

			if (isStreamResponse)
			{
				SLDocument document = SearchResultsAsExcel(dbResponse.Data.Results);
				// Select the first worksheet as the active one.
				var firstSheet = document.GetWorksheetNames()[0];
				document.SelectWorksheet(firstSheet);

				var stream = new MemoryStream();
				document.SaveAs(stream);

				response = createFileResponseMessage(HttpStatusCode.OK, "SearchResults.xlsx", stream.ToArray());
			}
			else
			{
				response = Request.CreateResponse(HttpStatusCode.OK, dbResponse.Data);
			}

			return ResponseMessage(response);
		}

		/// <summary>
		/// Typeahead search suggestions.
		/// </summary>
		/// <remarks>
		/// The typeahead search is indented to provide suggestions based on a partial search term.
		/// 
		/// This search looks for partial matches in Name and Tags.
		/// 
		/// If the partial search term appears to be a UID, the Asset UID and Tag UIDs are searched instead.
		/// </remarks>
		/// <param name="query">Query string</param>
		/// <param name="categories">Comma separated list of Categories to limit search to</param>
		/// <param name="num">Max number of results. Defaults to 7</param>
		/// <returns></returns>
		[
			HttpGet,
			Route("typeahead"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Search result suggestions based on query.", typeof(IList<SearchResult>)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your search request is invalid"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = false)
		]
		public async Task<IHttpActionResult> GetTypeaheads(string query, string categories = null, int? num = null)
		{
			//query = cleanPhrase(query);
			List<AssetTypeClass> limitedeCategories = null;
			if (!string.IsNullOrWhiteSpace(categories))
			{
				var categoryList = categories.Split(',')
					.Select(c => c.Trim())
					.Where(o => Enum.TryParse<AssetTypeClass>(o, out _))
					.Select(o => (AssetTypeClass)Enum.Parse(typeof(AssetTypeClass), o));

				var invalidCategories = categoryList.Except(GetVisibleCategories());

				if (invalidCategories.Any())
				{
					return errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidRequest, string.Format(Error.CategoryNotAvailable, string.Join(", ", invalidCategories)));
				}
				limitedeCategories = categoryList.ToList();
			}

			var response = await Search.ReadResultsAsync(query, false, true, false, false, limitedeCategories, null, 0, num ?? 7);
			loadSearchResultUris(response.Data.Results);
			return Ok(response.Data.Results);
		}

		/// <summary>
		/// List categories available for filtering in search
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("categories"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Categories available for filtering", typeof(List<string>)),
			ApiExplorerSettings(IgnoreApi = false)
		]
		public IHttpActionResult GetCategories()
		{
			var visibleCategories = GetVisibleCategories();
			return Ok(visibleCategories.Select(o => o.GetName().Replace(" ", "")));
		}

		/// <summary>
		/// List Categories and Asset Types that can be indexed
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("indexableTypes"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "List of indexable Categories and Asset Types", typeof(List<IndexableType>)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true),
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> GetIndexableTypes()
		{
			List<IndexableType> types = (await Company.QueryAsync<IndexableType>(
				@"SELECT at.Name, at.Class, at.Uid as AssetTypeUid, P.[Path] as AssetTypePath
				FROM [dbo].[AssetType] at
				cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' > ') P 
				WHERE EXISTS (SELECT 1 FROM [dbo].[Asset] a WHERE a.AssetTypeId = at.ID)")).ToList();
			
			List<IndexableType> classes = assetTypeClasses
				.Where(c => types.Any(at => at.Class == (int)c))
				.Select(c => new IndexableType { 
					Name = c.ToString(), 
					Class = (int)c, 
					AssetTypeUid = Guid.Empty, 
					ClassName = c.ToString() 
				}).ToList();

			//classes.Add(new IndexableType { Name = AssetTypeClass.SemanticType.ToString(), Class = (int)AssetTypeClass.SemanticType, AssetTypeUid = Guid.Empty, ClassName = AssetTypeClass.SemanticType.ToString() });
			classes.AddRange(types);

			//Reclassify Reference/ReferenceItemType
			//classes.Where((c) => c.Class == 9).ToList().ForEach((c) => c.Class = 14);

			return Ok(classes);
		}

		#region Enrich elastic results with DB data

		private static readonly List<AssetTypeClass> assetTypeClasses = new List<AssetTypeClass> {
			AssetTypeClass.BusinessAsset,
			AssetTypeClass.TechnicalAsset,
			AssetTypeClass.Diagram,
			AssetTypeClass.Model,
			AssetTypeClass.Policy,
			AssetTypeClass.Rule,
			AssetTypeClass.Group,
			AssetTypeClass.User,
			AssetTypeClass.Reference
		};

		private List<AssetTypeClass> GetVisibleCategories()
		{
			var exclude = new List<AssetTypeClass> { AssetTypeClass.Generic };
			if(!SecurityContext.IsAdministrator)
			{
				exclude.Add(AssetTypeClass.User);
				exclude.Add(AssetTypeClass.Group);
			}

			var visibleCategories = assetTypeClasses
				.Where(c => Company.AssetTypes.Any(at => at.Class == c && !exclude.Contains(at.Class)))
				.Select(c => c).ToList();

			//visibleCategories.Add(AssetTypeClass.SemanticType);

			return visibleCategories;
		}

		private string ValidateQueryRequest(QueryRequest queryRequest)
		{
			if (queryRequest.Size > 5000)
			{
				return string.Format(Error.SizeTooBig, 5000);
			}

			if (queryRequest.AggregationFilters.Any())
			{
				if (queryRequest.AggregationFilters.Exists(f => f.Class.HasValue))
				{
					IEnumerable<AssetTypeClass> categoryList = queryRequest.AggregationFilters.Where(f => f.Class.HasValue).Select(c => c.Class.Value);
					IEnumerable<AssetTypeClass> invalidCategories = categoryList.Except(GetVisibleCategories());
					if (invalidCategories.Any())
					{
						return string.Format(Error.CategoryNotAvailable, string.Join(", ", invalidCategories));
					}
				}
			}

			return "";
		}

		#endregion

		/// <summary>
		/// Parses the new full-text search results into an Excel payload.
		/// </summary>
		private SLDocument SearchResultsAsExcel(List<SearchResult> results)
		{
			SLDocument document = new SLDocument();

			AddSearchResultsSheet(document, "Search Results", results, null);

			//List<Guid> assetTypeUidWithFields = GetAssetTypeUidWithField(results);
			//assetTypeUidWithFields.ForEach(assetTypeUid =>
			//{
			//	AssetType assetType = Company.AssetTypes.Where(a => a.uid == assetTypeUid).FirstOrDefault();
			//	var fieldTypes = Company.Filter<FieldType>(f => f.AssetTypeID == assetType.ID && f.SearchAddToResult).ToList();

			//	AddSearchResultsSheet(document, assetType.Name, model.Results.Where(r => r.AssetTypeUid == assetTypeUid), fieldTypes);
			//});
			document.DeleteWorksheet(SLDocument.DefaultFirstSheetName);

			return document;
		}

		private void AddSearchResultsSheet(SLDocument document, string name, List<SearchResult> results, IEnumerable<FieldType> fieldTypes)
		{
			document.AddWorksheet(name);
			int index = 1;
			int rownum = 1;
			document.SetCellValue(1, index++, "Category");
			document.SetCellValue(1, index++, "Type");
			document.SetCellValue(1, index++, "Name");
			document.SetCellValue(1, index++, "Status");
			document.SetCellValue(1, index++, "Data Quality Score");
			document.SetCellValue(1, index++, "Governance Score");
			document.SetCellValue(1, index++, "Asset Path");
			document.SetCellValue(1, index++, "Asset Type Path");
			document.SetCellValue(1, index++, "Tags");

			fieldTypes?.ToList().ForEach(ft =>
			{
				document.SetCellValue(1, index++, ft.FriendlyName.GetSafeXLSColumnValue());
			});

			document.SetCellValue(1, index++, "Asset UID");
			document.SetCellValue(1, index++, "Asset Type UID");
			document.SetCellValue(1, index++, "URL");

			foreach (var res in results)
			{
				rownum++;
				index = 1;
				document.SetCellValue(rownum, index++, res.Group.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.Type.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.Name.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.Scores.Exists(s => s.ScoreType == ScoreType.DataQuality) ? res.Scores.Where(s => s.ScoreType == ScoreType.DataQuality).Select(s => s.Value).FirstOrDefault().ToString() : null);
				document.SetCellValue(rownum, index++, res.Scores.Exists(s => s.ScoreType == ScoreType.Governance) ? res.Scores.Where(s => s.ScoreType == ScoreType.Governance).Select(s => s.Value).FirstOrDefault().ToString() : null);
				//document.SetCellValue(rownum, index++, res.AssetPath == null ? "" : string.Join(" > ", res.AssetPath.Select(p => string.Join(" / ", p.Key))));
				//document.SetCellValue(rownum, index++, res.AssetPath == null ? "" : string.Join(" > ", res.AssetPath.Select(p => p.AssetType)));
				//document.SetCellValue(rownum, index++, res.Tags == null ? "" : string.Join("|", res.Tags?.Select(t => t.Value)).GetSafeXLSColumnValue());

				fieldTypes?.ToList().ForEach(ft =>
				{
					var field = res.Fields.Where(f => f.Name == ft.Name).FirstOrDefault();
					document.SetCellValue(rownum, index++, field?.Value.GetSafeXLSColumnValue());
				});

				document.SetCellValue(rownum, index++, res.Uid.ToString());
				document.SetCellValue(rownum, index++, res.AssetTypeUid.ToString());
				document.SetCellValue(rownum, index++, res.AbsoluteUrl);
			}

			for (int ci = 1; ci < index; ci++)
			{
				document.AutoFitColumn(ci);
			}
		}

		private struct SearchAugment : IEquatable<SearchAugment>
		{
			public SearchAugment(Guid guid, string status, string obj, long objectid, bool profile)
			{
				AssetUid = guid;
				Status = status;
				Object = obj;
				ObjectId = objectid;
				HasProfiling = profile;
			}

			public Guid AssetUid;
			public string Status;
			public string Object;
			public long ObjectId;
			public bool HasProfiling;

			public bool Equals(SearchAugment other)
			{
				return AssetUid.Equals(other.AssetUid);
			}
		}
	}
}
