using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using d360.core.entities;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
    [ApiVersion("2.0")]
    [RoutePrefix("api/v{version:apiVersion}/assets/types")]
    [Authorize]
    public class AssetTypesController : BaseV2ApiController
    {
        private IAssetTypeRepository AssetTypeRepository { get; }

        public AssetTypesController(ICoreComponentSet set, IAssetTypeRepository assetTypeRepository) : base(set)
        {
            AssetTypeRepository = assetTypeRepository;
        }

        [SwaggerProduces("application/json")]
        [SwaggerResponse(HttpStatusCode.OK, "Ancestry for a given asset type.", typeof(ICollection<AssetTypeAncestryModel>))]
        [SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse))]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))]
        [SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse))]
        [HttpGet]
        [Route("{assetTypeUid}/ancestry")]
        public async Task<IHttpActionResult> GetAncestry(Guid assetTypeUid, CancellationToken cancellationToken)
        {
            ValidateParameters();

            var entities = await AssetTypeRepository.GetAncestryAsync(assetTypeUid, cancellationToken);
            if (entities.Count == 0)
            {
                throw new NotFoundBusinessLayerException($"{nameof(AssetType)} with uid=\"{assetTypeUid}\" not found.");
            }

            var result = entities.Select(x => new AssetTypeAncestryModel
            {
                uid = x.uid,
                Name = x.Name
            }).ToArray();

            return Ok(result);
        }

        public class AssetTypeAncestryModel
        {
            [DataMember]
            public Guid uid { get; set; }

            [DataMember]
            public string Name { get; set; }
        }
    }
}
