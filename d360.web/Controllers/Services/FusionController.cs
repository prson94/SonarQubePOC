using d360.core;
using d360.core.entities;
using d360.core.entities.api;
using d360.core.entities.Views;
using d360.core.exceptions;
using d360.extensions;
using d360.model;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Linq;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling third-party metadata synchronization.
    /// </summary>
    [ApiVersionNeutral, RoutePrefix("services/fusion"), Authorize, Name("Fusion Service"), ApiExplorerSettings(IgnoreApi = true)]
    public class FusionController : BaseApiController
    {
        #region DI

        IStorageProvider Storage;
        IQueueSource Queue;

        public FusionController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IQueueSource queue)
            : base(community, company)
        {
            Storage = storage;
            Queue = queue;
        }

        #endregion

        [Route("attributetypes")]
        public IQueryable<dynamic> GetFusionAttributeTypes()
        {
            return Company.FusionAttributeTypes.Select(i =>
            new
            {
                ID = i.ID,
                ParentID = i.ParentID,
                TextPath = i.TextPath,
                Name = i.Name
            }).AsQueryable();
        }

        /// <summary>
        /// Get a specific fusion configuration.  This configuration will provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>The specific configuration.</returns>
        [Route("{typeID:int}/configurations/{id:int}")]
        public HttpResponseMessage GetConfiguration(int typeID, int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the fusion configuration details.");
            }

            var model = Company.GetFusionAsDictionary(id);
            if (model == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            return Request.CreateResponse<Dictionary<string, object>>(HttpStatusCode.OK, model);
        }
    }
}
