using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using d360.web.Services;

using Microsoft.Web.Http;
using Newtonsoft.Json;
using repositories;
using Resources;

using SpreadsheetLight;

using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
	/// <summary>
	/// This service houses all endpoints handling tag management in Govern
	/// </summary>
	[
		ApiVersion("2.0"),
		RoutePrefix("api/v{version:apiVersion}/tags"),
		Authorize
	]
	public class TagsController : BaseV2ApiController
	{
		private readonly ITagRepository tagRepository;
		private readonly IAssetRepository assetRepository;
		private readonly IQueueSource Queue;

		public TagsController(ICoreComponentSet set, IQueueSource queue, ITagRepository repository, IAssetRepository assetRep) : base(set)
		{
			Queue = queue;
			tagRepository = repository;
			assetRepository = assetRep;
		}

		/// <summary>
		/// Returns all tags that are defined in Govern that match the search criteria.          
		/// </summary>        
		/// <returns>A list of tags</returns>
		[
			HttpGet, MapToApiVersion("2.0"),
			Route("search"),
			SwaggerProduces("application/json"),
			SwaggerParameter("Value", "The value of the tag that's to be searched.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("TagTypeUid", "The UID for the type of the tags that's to be searched. Defaults to searching 'General'.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("TagTypeId", "The Id for the type of the tags that's to be searched. Defaults to searching 'General'.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerResponse(HttpStatusCode.OK, "Search for tags completed.", typeof(List<dynamic>)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Error while fetching tags.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> Search()
		{
			var queryParams = Request.GetQueryNameValuePairs();
			var response = await Catalog.SearchTags(queryParams);
			return (response.IsSuccess) ? 
				Ok(response.Data) : 
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}

		/// <summary>
		/// Retrieves a list of all tags.
		/// </summary>                
		[HttpGet]
		[MapToApiVersion("2.0")]
		[Route("")]
		[SwaggerProduces("application/json")]
		[SwaggerParameter("uid", "The Uid of a specific tag to return.", DataType = "string", ParameterType = "query", Required = false)]
		[SwaggerParameter("tagtypeuid", "The Uid of a specific tag type to return.", DataType = "string", ParameterType = "query", Required = false)]
		[SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false)]
		[SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false)]
		[SwaggerParameter("_tag", "Search term that filters on the name of the tag.", DataType = "string", ParameterType = "query", Required = false)]
		[SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by tag name.", DataType = "string",
			ParameterType = "query", Required = false)]
		[SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.",
			DataType = "string", ParameterType = "query", Required = false)]
		[SwaggerParameter("_includeTotal",
			"Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.",
			DataType = "boolean", ParameterType = "query", Required = false)]
		[SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(PagedApiBaseViewModel<TagApiModelWrapper>))]
		[SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse))]
		public async Task<IHttpActionResult> Get()
		{
			var queryParams = Request.GetQueryNameValuePairs();
			string isValid = isPageSizeAndNumValid(queryParams);

			if (!string.IsNullOrEmpty(isValid))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid);
			}

			var response = await Catalog.ReadTagsAsync(queryParams);

			return (response.IsSuccess) ?
				Ok(response.Data) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}

		/// <summary>
		/// Deletes a tag based on the provided Uid.
		/// </summary>
		/// <param name="tagUid">The uid of the tag to be removed.</param>
		/// <param name="cascade">Cascade, if true a tag that is applied to an asset will be deleted along with the association.  If false a tag that is in use will not be deleted.  The default is false for the cascade setting.</param>
		/// <returns>A status for the DELETE request.</returns>
		[
			HttpDelete,
			Route("{tagUid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag provided is invalid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> DeleteById(string tagUid, bool cascade = false)
		{
			Guid _tagUid;
			if (!Guid.TryParse(tagUid, out _tagUid))
			{
				return errorMessageArgumentResponse(string.Format(ApiMessages.InvalidGuid, tagUid));
			}
			if (!tagRepository.DoesTagExists(_tagUid))
			{
				return errorMessageNotFoundResponse(string.Format(TagsApiMessages.TagUidNotFound, tagUid));
			}
			if (!tagRepository.IsAuthorizedToEditTag(_tagUid))
			{
				return errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied);
			}

			if (!cascade)
			{
				List<Guid> uidscascade = new List<Guid> { _tagUid};
				var cascadeErrorMessage = tagRepository.CheckTagAssetbyUids(uidscascade);
				if (!string.IsNullOrWhiteSpace(cascadeErrorMessage))
				{
					return errorMessageResponse(HttpStatusCode.BadRequest, cascadeErrorMessage);
				}
			}


			var response = await Catalog.RemoveTagsAsync(new List<Guid> { _tagUid });
			return (response.IsSuccess) ? 
				successMessageResponse(HttpStatusCode.OK, TagsApiMessages.TagRemoved, TagsApiMessages.TagRemoveMessage) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message, response.Message);
		}


		/// <summary>
		/// Adds a tag with the properties provided in the model.
		/// </summary>        
		/// <param name="model">The tag to be created.</param>
		/// <returns>The created tag.</returns>
		[
			HttpPost,
			Route(""),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "The specified tag was saved, returns the properties of the created tag.", typeof(TagApiModel)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> PostTag(TagApiUpsertModel model)
		{
			if (model == null)
			{
				return errorMessageArgumentResponse(ApiMessages.ErrorInvalidDatasetMessage);
			}

			var response = await Catalog.CreateTagAsync(model.Value, model.TagTypeUid);
			return (response.IsSuccess) ?
				Ok(response.Data) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}

		/// <summary>
		/// Updates the specified tag with the values provided in the model.
		/// </summary>
		/// <param name="tagUid">The Uid of the tag to be updated.</param>        
		/// <param name="model">The new definition of the tag to be used for the update.</param>
		/// <returns>A tag model representing the updated tag.</returns>
		[
			HttpPut,
			MapToApiVersion("2.0"),
			Route("{tagUid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "The specified tag was updated, returns the properties of the created tag.", typeof(TagApiModel)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> Put(string tagUid, TagApiUpsertModel model)
		{
			Guid tagId;
			if (!Guid.TryParse(tagUid, out tagId))
			{
				return errorMessageArgumentResponse(ApiMessages.InvalidGuid);
			}

			if (!tagRepository.DoesTagExists(tagId))
			{
				return errorMessageNotFoundResponse(string.Format(TagsApiMessages.TagUidNotFound, tagUid));
			}

			if (!tagRepository.IsAuthorizedToEditTag(tagId))
			{
				return errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
			}

			var response = await Catalog.UpdateTagAsync(tagId, model.Value);

			if (response.IsSuccess)
			{
				await Queue.CreateMessageAsync(constants.Queue.Search, new ReindexModel
				{
					To = QueueAction.UpdateInIndex,
					BatchOperation = ReindexBatchOperation.Update,
					BatchUids = new List<Guid> { tagId }
				});

				var result = await Catalog.ReadTagAsync(tagId);
				return Ok(result.Data);
			}
			else
			{
				return errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
			}
		}

		/// <summary>
		/// Allows you to remove tags based on a tag list.
		/// </summary>
		/// <remarks>
		/// Use the cascade flag set to true to delete a tag that is applied to an asset that tag will be deleted along with the association.  If false a tag that is in use will not be deleted.  The default is false for the cascade setting.
		/// </remarks>
		/// <returns>A status for the DELETE request.</returns>
		[
			HttpDelete,
			Route(""),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate an invalid model was provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> DeleteTags(List<TagApiDeleteModel> model)
		{
			if (model == null)
			{
				return errorMessageArgumentResponse(ApiMessages.ErrorInvalidDatasetMessage);
			}
			foreach (var item in model)
			{
				if (!tagRepository.DoesTagExists(item.uid))
				{
					return errorMessageNotFoundResponse(string.Format(TagsApiMessages.TagUidNotFound, item.uid.ToString()));
				}

				if (!tagRepository.IsAuthorizedToEditTag(item.uid))
				{
					return errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied);
				}
			}


			var uidscascade = model.Where(x=>x.cascade == false).Select(o => o.uid).ToList();

			if (uidscascade.Count > 0)
			{
				var cascadeErrorMessage = tagRepository.CheckTagAssetbyUids(uidscascade);
				if (!string.IsNullOrWhiteSpace(cascadeErrorMessage))
				{
					return errorMessageResponse(HttpStatusCode.BadRequest, cascadeErrorMessage);
				}
			}

			var uids = model.Select(o => o.uid).ToList();
			var response = await Catalog.RemoveTagsAsync(uids);
			return (response.IsSuccess) ?
				successMessageResponse(HttpStatusCode.OK, TagsApiMessages.TagRemoveTitle, TagsApiMessages.TagRemoveMessage) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message, response.Message);
		}

		/// <summary>
		/// Consolidates tags
		/// </summary>
		/// <param name="parentUid">The unique identifier of the parent tag.</param>        
		/// <param name="childrenUids">The list of children tags which we want to consolidate.</param>
		/// <returns>A status for the POST request.</returns>
		[
			HttpPost,
			Route("consolidate/{parentUid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<TagApiModel>)),
			SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true),
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> ConsolidateTags(string parentUid, List<string> childrenUids)
		{
			Guid _parentUid;
			if (!Guid.TryParse(parentUid, out _parentUid))
			{
				return errorMessageArgumentResponse(string.Format(ApiMessages.CustomUidNotValid, parentUid));
			}

			var _childrenUids = new List<Guid>();
			foreach (var item in childrenUids)
			{
				Guid child;
				if (Guid.TryParse(item, out child))
				{
					_childrenUids.Add(child);
				}
				else
				{
					return errorMessageArgumentResponse(string.Format(ApiMessages.CustomUidNotValid, item));
				}
			}

			if (_childrenUids.Contains(_parentUid))
			{
				return errorMessageArgumentResponse(TagsApiMessages.ParentNotIncludeInChild);
			}

			var response = await Catalog.ConsolidateTagsAsync(_parentUid, _childrenUids);

			return response.IsSuccess ? 
				Ok(response.Data) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}

		[HttpGet, MapToApiVersion("2.0"), Route("{tagUid}/assetpath"), ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IHttpActionResult> GetAssetsPath(Guid tagUid)
		{
			var result = await Catalog.ReadAssetBreadcrumbsByTagAsync(tagUid);
			return Ok(result.Data);
		}

		/// <summary>
		/// GET a list of tags.
		/// </summary>
		/// <returns>A excel file containing tags.</returns>
		[
			HttpGet,
			MapToApiVersion("2.0"),
			ApiExplorerSettings(IgnoreApi = true),
			Route("export"),
			FileDownload,
			RequireAdminPermissions,
			SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
			SwaggerResponse(HttpStatusCode.OK, "Exported tags to Excel.", typeof(List<TagApiModel>)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> ExportToExcel()
		{
			var queryParams = Request.GetQueryNameValuePairs();
			var tags = await tagRepository.GetTagsForExcel(queryParams);
			var document = new SLDocument();
			document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

			#region Create the list sheet

			#region Header

			int index = 1;
			document.SetCellValue(1, index++, "Uid");
			document.SetCellValue(1, index++, "Name");
			document.SetCellValue(1, index++, "Use Count");
			document.SetCellValue(1, index++, "Created On");
			document.SetCellValue(1, index++, "Created By");
			document.SetCellValue(1, index++, "Updated On");
			document.SetCellValue(1, index++, "Updated By");

			#endregion

			int rowNumber = 1;
			foreach (var row in tags)
			{
				index = 1;
				rowNumber++;
				document.SetCellValue(rowNumber, index++, row.uid.ToString());
				document.SetCellValue(rowNumber, index++, row.Value.ToString());
				document.SetCellValue(rowNumber, index++, row.UseCount.ToString());
				document.SetCellValue(rowNumber, index++, row.CreatedOn.ToString());
				document.SetCellValue(rowNumber, index++, row.CreatedBy is null ? "" : row.CreatedBy.ToString());
				document.SetCellValue(rowNumber, index++, row.UpdatedOn.ToString());
				document.SetCellValue(rowNumber, index++, row.UpdatedBy is null ? "" : row.UpdatedBy.ToString());
			}

			#endregion

			var stream = new System.IO.MemoryStream();
			document.SaveAs(stream);

			var result = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent(stream.GetBuffer())
			};
			result.Content.Headers.ContentLength = stream.Length;
			result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
			{
				FileName = string.Format("Tags {0}.xlsx", System.DateTime.Now.ToShortDateString())
			};
			result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

			return ResponseMessage(result);
		}

		/// <summary>
		/// Gets tag details.
		/// </summary>
		/// <param name="uid">The unique identifier of the tag.</param>        
		/// <returns>Tag details.</returns>
		[
			HttpGet,
			MapToApiVersion("2.0"),
			Route("{uid}/details"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "The specified tag was updated, returns the properties of the created tag.", typeof(TagDetailApiModel)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Request is badly formatted or has failed validation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("sortorder", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("sortby", "The name of the field to order results [Allowed fields are displayvalue, assettype, tagsasstring, assetid]. By default the results are ordered by DisplayValue asc", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is false meaning the total count is not included.", DataType = "boolean", ParameterType = "query", Required = false),
			SwaggerParameter("DisplayValue", "Filter by Display Value.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("AssetType", "Filter by Asset Type.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("TagsasString", "Filter by Tags as string.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("AssetTypeUid", "Filter by Asset Type Uid.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("globalSearch", "Filter by DisplayValue or AssetType or TagsasString. When global search parameter use then filter specific parameter defined for DisplayValue, AssetType, TagsasString not consider", DataType = "string", ParameterType = "query", Required = false),
		]
		public IHttpActionResult GetTagDetails(string uid)
		{
			Guid tagUid = Guid.Parse(uid);
			Guid AssetTypeUid = new Guid();

			if (!tagRepository.DoesTagExists(tagUid))
			{
				return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.InvalidRequest, string.Format(TagsApiMessages.TagUidNotFound, tagUid.ToString()));
			}

			var queryParams = Request.GetQueryNameValuePairs();
			string isValid = isPageSizeAndNumValid(queryParams);

			if (!string.IsNullOrEmpty(isValid))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid);
			}

			if (queryParams.Any(q => q.Key.ToLower() == "assettypeuid"))
			{
				if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value.ToLower(), out AssetTypeUid))
				{
					return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidAssetTypeID);
				}

				if (AssetTypeUid != null && AssetTypeUid != Guid.Empty)
				{
					var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == AssetTypeUid);

					if (assetType == null)
					{
						return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ActionApiMessages.AssetTypeNotFound, AssetTypeUid.ToString()));
					}
				}
				else
				{
					return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidAssetTypeID);
				}
			}

			var results = tagRepository.GetDetails(tagUid, queryParams);
			return Ok(results);
		}

		/// <summary>
		/// GET a list of tagged assets by tag Uid.
		/// </summary>
		/// <returns>A excel file containing tagged assets.</returns>
		[
			HttpGet,
			MapToApiVersion("2.0"),
			Route("{tagUid}/export"),
			FileDownload,
			SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
			SwaggerResponse(HttpStatusCode.OK, "Exported tags to Excel.", typeof(List<TagApiModel>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public IHttpActionResult ExportTagToExcel(string tagUid)
		{
			Guid uid = Guid.Parse(tagUid);
			var tag = Company.Tags.FirstOrDefault(x => x.uid == uid);
			var queryParams = Request.GetQueryNameValuePairs();
			var tags = tagRepository.GetDetails(uid, queryParams);
			var document = new SLDocument();
			document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

			#region Create the list sheet

			#region Header

			int index = 1;
			document.SetCellValue(1, index++, "Asset");
			document.SetCellValue(1, index++, "Asset Type");
			document.SetCellValue(1, index++, "Tags");
			document.SetCellValue(1, index++, "Added By");
			document.SetCellValue(1, index, "Date Added");

			#endregion

            int rowNumber = 1;
            foreach (var row in tags.items)
            {
                index = 1;
                rowNumber++;
                var tagDetails = row.Tags.SingleOrDefault(t => t.uid == uid);
                document.SetCellValue(rowNumber, index++, row.DisplayValue.GetSafeXLSColumnValue());
                document.SetCellValue(rowNumber, index++, $"{row.AssetType}");
                document.SetCellValue(rowNumber, index++, $"{string.Join("|", row.Tags.Select(x => x.Value))}");
                document.SetCellValue(rowNumber, index++, $"{tagDetails.CreatedByFirstName} {tagDetails.CreatedByLastName}");
                document.SetCellValue(rowNumber, index, $"{tagDetails.CreatedOn}");
            }

			#endregion

			var stream = new System.IO.MemoryStream();
			document.SaveAs(stream);

			var result = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent(stream.GetBuffer())
			};
			result.Content.Headers.ContentLength = stream.Length;
			result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
			{
				FileName = string.Format("{1} {0}.xlsx", DateTime.Now.ToShortDateString(), tag.Value)
			};
			result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

			return ResponseMessage(result);
		}

		[HttpGet, MapToApiVersion("2.0"), Route("{tagUid:Guid}/tooltip"), ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetTagTooltipData(Guid tagUid, Guid? assetUid = null)
		{
			var result = tagRepository.GetTooltip(tagUid, assetUid);
			return Ok(result);
		}

		[HttpGet, MapToApiVersion("2.0"), Route("tooltipByName"), ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetTagTooltipByNameData(string tagName, Guid? assetUid = null)
		{
			tagName = tagName.Replace("&amp;", "&");
			var tag = tagRepository.GetTagByName(tagName);
			return GetTagTooltipData(tag.uid, assetUid);
		}

		/// <summary>
		/// A check to see if a tag already exists or not.
		/// </summary>
		/// <param name="value">The name of the tag that's been checked if exists.</param>
		[HttpGet, Route("exists"), SwaggerProduces("application/json"),
		SwaggerResponse(HttpStatusCode.OK, "Tag does exist.", typeof(HttpStatusCode)),
		SwaggerResponse(HttpStatusCode.NotFound, "Tag doesn't exist.", typeof(ErrorResponse))]
		public IHttpActionResult CheckIfTagExist(string value, Guid? tagTypeUid = null)
		{
			var result = tagRepository.DoesTagExists(value, tagTypeUid);
			return (result == false) ? errorMessageNotFoundResponse("") : Ok();
		}

		[HttpGet, Route("AssetTagDetails"), ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult getAssetTagDetails(int tagID, Guid assetUID)
		{
			var asset = assetRepository.GetAssetByUID(assetUID);
			if (asset == null)
			{
				return errorMessageNotFoundResponse("Asset not found.");
			}
			var result = tagRepository.GetAssetTagDetails(tagID, asset.ID);
			return Ok(result);
		}

		[HttpGet,
		Route("permissions/{assetUid:Guid}"),
		ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult getAssetTagPermissions(Guid assetUid)
		{
			try
			{
				var result = new List<TagPermissionItem>();

				if (assetUid == null || assetUid == Guid.Empty)
				{
					return Ok(result);
				}

				var asset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);
				if (asset == null)
				{
					return Ok(result);
				}

				List<AssetTag> assetTags = Company.AssetTags.Where(x => x.AssetID == asset.ID).ToList();
				int[] tagIDs = assetTags.Select(x => x.TagID).ToArray();
				var tags = Company.Tags.Where(x => tagIDs.Contains(x.ID)).ToList();

				if (Company.HasAssetPermission(asset.ID, Permission.AddAsset) || Company.HasAssetPermission(asset.ID, Permission.EditAsset) || SecurityContext.IsAdministrator)
				{
					foreach (var tag in tags)
					{
						result.Add(new TagPermissionItem()
						{
							CanDelete = true,
							uid = tag.uid,
							Value = tag.Value
						});
					}
				}
				else
				{
					foreach (var tag in tags)
					{
						result.Add(new TagPermissionItem()
						{
							CanDelete = tag.CreatedBy == SecurityContext.ResourceID,
							uid = tag.uid,
							Value = tag.Value
						});
					}
				}

				return Ok(result);
			}
			catch (Exception e)
			{
				return errorMessageArgumentResponse(TagsApiMessages.ErrorTagPermissionDetail);
			}
		}

		[
			HttpGet,
			Route("AssetTagOwnerByName"),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public IHttpActionResult getAssetTagOwnerByName(string tagName, Guid assetUID)
		{
			try
			{
				tagName = tagName.Replace("&amp;", "&");
				var tag = tagRepository.GetTagByName(tagName);
				var asset = assetRepository.GetAssetByUID(assetUID);
				var result = tagRepository.GetAssetTagDetails(tag.ID, asset.ID);

				return Ok(result);
			}
			catch (Exception e)
			{
				return errorMessageArgumentResponse(TagsApiMessages.ErrorAssetTagDetail);
			}
		}

		/// <summary>
		/// Retrieves a list of all tag types.
		/// </summary>                
		[HttpGet]
		[MapToApiVersion("2.0")]
		[Route("tagTypes")]
		[SwaggerProduces("application/json")]
		[SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(List<TagTypeApiModel>))]
		[SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))]
		public async Task<IHttpActionResult> GetTagTypes()
		{
				var models = await Catalog.ReadTagTypesAsync();
				return Ok(models);
		}

		/// <summary>
		/// Retrieves a list of all tag types for an asset type.
		/// </summary>                
		[HttpGet]
		[MapToApiVersion("2.0")]
		[Route("tagTypesForAssetType/{assetTypeUid:Guid}/{name?}")]
		[SwaggerProduces("application/json")]
		[SwaggerResponse(HttpStatusCode.OK, "A full list of tags for an asset type.", typeof(List<TagTypeApiModel>))]
		[SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))]
		public async Task<IHttpActionResult> GetTagTypesForAssetType(Guid assetTypeUid,string name = null)
		{
			
			var models = await Catalog.ReadTagTypesAsync(assetTypeUid,name);
			return Ok(models);
		}

		/// <summary>
		/// Adds a tag type with the properties provided in the model.
		/// </summary>        
		/// <param name="model">The tag type to be created.</param>
		/// <returns>The created tag type.</returns>
		[
			HttpPost,
			Route("tagTypes"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "The specified tag type was saved, returns the properties of the created tag type.", typeof(TagTypeApiModel)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you are not allowed to perform this action.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> PostTagType(TagTypeApiUpsertModel model)
		{
			if (model == null)
			{
				return errorMessageArgumentResponse(ApiMessages.ErrorInvalidDatasetMessage);
			}
			var response = await Catalog.CreateTagTypeAsync(model.Value);
			return (response.IsSuccess) ?
				Ok(response.Data) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}

		/// <summary>
		/// Updates the specified tag type with the values provided in the model.
		/// </summary>
		/// <param name="tagTypeUid">The Uid of the tag type to be updated.</param>        
		/// <param name="model">The new definition of the tag type to be used for the update.</param>
		/// <returns>A tag type model representing the updated tag.</returns>
		[
			HttpPut,
			MapToApiVersion("2.0"),
			Route("tagTypes/{tagTypeUid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "The specified tag type was updated, returns the properties of the created tag type.", typeof(TagTypeApiModel)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag type was not found.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> PutTagType(string tagTypeUid, TagTypeApiUpsertModel model)
		{
			Guid tagTypeId;
			if (!Guid.TryParse(tagTypeUid, out tagTypeId))
			{
				return errorMessageArgumentResponse(ApiMessages.InvalidGuid);
			}

			if (model == null)
			{
				return errorMessageArgumentResponse(ApiMessages.Invalid);
			}

			var response = await Catalog.UpdateTagTypeAsync(tagTypeId, model.Value);
			if (response.IsSuccess)
			{
				var responseModel = await Catalog.ReadTagTypeAsync(tagTypeId);
				return Ok(responseModel);
			}
			else
			{
				return errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
			}
		}

		/// <summary>
		/// Deletes a tag type based on the provided Uid.
		/// </summary>
		/// <param name="tagTypeUid">The uid of the tag type to be removed.</param>
		/// <param name="cascade">Cascade, if true, all tags of this tag type even when those tags are applied to a assets will be deleted along with the associations.  If false a tag type that has tags in use will not be deleted.  The default is false for the cascade setting.</param>
		/// <returns>A status for the DELETE request.</returns>
		[
			HttpDelete,
			Route("tagTypes/{tagTypeUid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag type provided is invalid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag type was not found.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> DeleteTagTypeById(string tagTypeUid, bool cascade = false)
		{
			Guid _tagTypeUid;
			if (!Guid.TryParse(tagTypeUid, out _tagTypeUid))
			{
				return errorMessageArgumentResponse(string.Format(ApiMessages.InvalidGuid, _tagTypeUid));
			}
			if (!tagRepository.DoesTagTypeExists(_tagTypeUid))
			{
				return errorMessageNotFoundResponse(string.Format(TagsApiMessages.TagTypeUidNotFound, _tagTypeUid));
			}
			var response = await Catalog.RemoveTagTypesAsync(new List<Guid> { _tagTypeUid });
			return response.IsSuccess ?
				successMessageResponse(HttpStatusCode.OK, TagsApiMessages.TagTypeRemoved, TagsApiMessages.TagTypeRemoveMessage) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}
	}
}
