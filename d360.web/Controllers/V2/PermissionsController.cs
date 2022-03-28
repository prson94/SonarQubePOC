using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

using d360.core.entities;
using d360.core.entities.Permissions;
using d360.core.enums;
using d360.core.resources;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;

using Microsoft.Web.Http;

using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// Retrieves Permissions for a given asset or asset type
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/permissions"),
        Authorize,
        StringEnumController
    ]
    public class PermissionsController : BaseV2ApiController
    {
        #region DI

        private readonly IAssetRepository AssetRepository;

        public PermissionsController(ICoreComponentSet set, IAssetRepository repository) : base(set)
        {
            AssetRepository = repository;
        }

        #endregion

        /// <summary>
        /// Get a list of permissions for a given asset
        /// </summary>        
        /// <param name="assetUid">The Uid of the asset</param>
        /// <returns>Returns a list of permissions for the asset</returns>
        [
            HttpGet,
            Route("asset/{assetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Assets of this Type do not support permissions.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset permissions.", typeof(PermissionsResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetPermissionsByUid(Guid assetUid)
        {
            Asset asset = AssetRepository.GetAssetByUID(assetUid);

            if (asset != null)
            {
                if (SupportsPermissions(asset.AssetType.Class))
                {
                    List<PermissionInfo> permissions = Company.GetPermissions(asset.ID, asset.AssetTypeID);
                    if (!Company.CurrentResourceIsAdmin && permissions.Count == 0)
                    {
                        //If there are no set responsibilities, non admin by default has ReadAccess rights to an asset
                        permissions.Add(Permission.ReadAsset.GetPermissionInfo());
                    }

                    return await Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, CreatePermissionsResponse(permissions)));
                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(Permissions.Permissions_Not_Supported)));
                }
            }
            else
            {
                return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(Permissions.UID_not_Found, "Asset")));
            }
        }

        /// <summary>
        /// Get a list of permissions for a given asset type
        /// </summary>        
        /// <param name="assetTypeUid">The Uid of the asset type</param>
        /// <returns>Returns a list of permissions for the asset type</returns>
        [
            HttpGet,
            Route("assettype/{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.NotFound, "AssetType not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "AssetType does not support permissions.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of assettype permissions.", typeof(PermissionsResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetTypePermissionsByUid(Guid assetTypeUid)
        {
            AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

            if (assetType != null)
            {
                if (SupportsPermissions(assetType.Class))
                {
                    List<PermissionInfo> permissions = Company.GetTypePermissions(assetType.Object, assetType.ObjectID);

                    return await Task.FromResult(Request.CreateResponse(HttpStatusCode.OK, CreatePermissionsResponse(permissions)));
                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(Permissions.AssetType_Permissions_Not_Supported, assetType.Name)));
                }

            }
            else
            {
                return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(Permissions.UID_not_Found, "AssetType")));
            }
        }

        /// <summary>
        /// Create the response object for permissions request.
        /// </summary>        
        /// <param name="objectPermissions">List of permissions on the Asset or AssetType</param>
        /// <returns>Returns a list of Permissions types and flag to indicate if active</returns>
        private Dictionary<string, bool> CreatePermissionsResponse(List<PermissionInfo> objectPermissions)
        {
            // get the complete list of permissions 
            List<PermissionInfo> permissions = Permission.DeleteAsset.GetList();

            Dictionary<string, bool> permissionsList = new Dictionary<string, bool>();

            // mark true for any matching entries in the passed permissions list.
            bool isAdmin = Company.CurrentResourceIsAdmin;
            permissions.ForEach(p =>
            {
                p.Selected = objectPermissions.Exists(t => t.ID == p.ID);
                permissionsList.Add(p.ID.ToString(), isAdmin ? isAdmin : p.Selected);
            });

            return permissionsList;
        }

        /// <summary>
        /// Check if the Asset Type supports permissions
        /// </summary>        
        /// <param name="assetTypeClass">The Class of the asset type</param>
        /// <returns>Returns true if permissions are supported</returns>
        private bool SupportsPermissions(AssetTypeClass assetTypeClass)
        {
            if (new[] { AssetTypeClass.Generic, AssetTypeClass.Organization, AssetTypeClass.User, AssetTypeClass.Group }.Contains(assetTypeClass))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
