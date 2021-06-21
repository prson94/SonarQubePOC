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

        [Route("")]
        public IQueryable<FusionType> GetTypes()
        {
            return Company.Query<FusionType>("select A.ID as AssetTypeID, A.uid as uid, T.* from FusionType T inner join AssetType A on A.Object = 'FusionType' and A.ObjectID = T.ID").AsQueryable();
        }

        [Route("{id:int}/attributetypes")]
        public IQueryable<FusionAttributeType> GetFusionAttributeTypes(int id)
        {
            return Company.Query<FusionAttributeType>(@"
with C as
(
	select FR.* from FusionAttributeType FR where ParentID is null and FR.FusionTypeID = @id
	union all
	select F.* from FusionAttributeType F
	inner join C on C.ID = F.ParentID
)
select 
	C.ID,
	C.ParentID,
	C.[Name],
	C.FusionTypeID,
	C.ScanEnabled,
	C.UpdatedBy,
	C.UpdatedOn,
	T.ID as AssetTypeID,
    T.uid as uid
from C
inner join AssetType T on T.[Object] = 'FusionAttributeType' and T.ObjectID = C.ID
order by C.ParentID, C.[Name]", new { id }).AsQueryable();
        }

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
        /// Get all available fusion configurations for a specific type.  These configurations provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>A list of available fusion configurations.</returns>
        [Route("{id:int}/configurations")]
        public HttpResponseMessage GetConfigurationsByType(int id, bool useFieldName = true)
        {
            const int markitFusionTypeId = 13;
            const string markitLineageSettingKey = "UseNewMarkitLineageGeneration";

            var showLineageButton = "cast(0 as bit) as ShowLineageButton,\n";
            var joins = "";
            var columns = "";

            getDynamicFieldJoinStatements(id, "Fusion", out joins, out columns, true, useFieldName);

            if (id == markitFusionTypeId)
            {
                if (Community.GetCompanySettings().TryGetValue(markitLineageSettingKey, out string val))
                {
                    if (val.Trim().ToLower() == "true")
                    {
                        showLineageButton = "cast(1 as bit) as ShowLineageButton,\n";
                    }
                }
            }

            columns += showLineageButton;


            var querySql = string.Format(@"select	A.ID,
		A.Name,
		A.Description,
        A.FusionTypeID,
		T.Name as FusionType,
        substring(
        (
            select	',' + ASST.Name + ':' + D.DisplayValue  AS [text()]
            from	FusionOwner [FO]
					inner join Asset ASS on ASS.ID = [FO].AssetID and [FO].FusionID = A.ID 
					inner join AssetType ASST on ASS.AssetTypeID = ASST.ID
					cross apply GetAssetDisplayValueById(ASS.ID) D
            ORDER BY D.DisplayValue
            For XML PATH ('')
        ), 2, 1000) as Owners,
        {0}
		A.Enabled
from	Fusion A {1} 
left join FusionType T on T.ID = A.FusionTypeID
where A.FusionTypeID = @id", columns, joins);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            return Request.CreateResponse(HttpStatusCode.OK, Company.Query<dynamic>(sql, new { id = id }));
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


        /// <summary>
        /// Gets the next configuration in the schedule that an agent may execute.
        /// </summary>
        /// <returns></returns>
        [Route("configurations/schedule")]
        public HttpResponseMessage GetNextConfigurationInSchedule()
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the scheduled fusion configuration.");

            Trace.TraceInformation("Fusion.GetNextConfigurationInSchedule BEGIN");

            var model = Company.Filter<FusionStatusLog>(fs => !fs.DateCompleted.HasValue && string.IsNullOrEmpty(fs.MachineQueuedOn)).OrderBy(fs => fs.DateStarted).Select(fs => new { fs.ID, fs.MachineQueuedOn, fs.Success, fs.Fusion }).Take(1).FirstOrDefault();

            if (model != null)
            {
                Trace.TraceInformation("Fusion.GetNextConfigurationInSchedule => Sending the following config down : {0} - {1}", model.Fusion.ID, model.Fusion.Name);

                var sType = SystemObjects.Fusion.ToString();
                var fields = Company.Filter<FieldWithRelation>(i => i.ObjectType == sType && i.ObjectID == model.Fusion.ID).ToList();

                var dictionary = new Dictionary<string, object>();

                dictionary.Add("ID", model.ID);
                dictionary.Add("FusionID", model.Fusion.ID);
                dictionary.Add("FusionTypeID", model.Fusion.FusionTypeID);
                foreach (var n in fields.OrderBy(f => f.SortOrder))
                {
                    dictionary.Add(n.Name, n.Value);
                }
                if (model.Fusion.ForceRefresh.HasValue)
                {
                    if (model.Fusion.ForceRefresh.Value)
                    {
                        dictionary.Add("ForceRefresh", model.Fusion.ForceRefresh.ToString().ToLower());
                    }
                }

                Trace.TraceInformation("Fusion.GetNextConfigurationInSchedule END");
                return Request.CreateResponse<Dictionary<string, object>>(HttpStatusCode.OK, dictionary);
            }
            else
            {
                Trace.TraceInformation("Fusion.GetNextConfigurationInSchedule END");
                return Request.CreateResponse(HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Allows authorized fusion agents to take an open item from the schedule to work on, thereby reserving that item so no other agent can work on it.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>An Http Status code</returns>
        [HttpPut, Route("configurations/schedule")]
        public HttpResponseMessage AssignOrCompleteAvailableConfigurationSchedule(FusionConfigurationScheduleRequestModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update the scheduled fusion configuration.");

            var prefix = "Fusion.AssignOrCompleteAvailableConfigurationSchedule => ";
            var errorMessage = "";

            if (model == null)
            {
                errorMessage = "You have not provided a valid schedule.";
                Trace.TraceWarning("{0}{1}", prefix, errorMessage);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, errorMessage);
            }

            var log = Company.GetById<FusionStatusLog>(model.ID, i => i.Fusion);

            if (log == null)
            {
                errorMessage = "No valid schedule located.";
                Trace.TraceWarning("{0}{1}", prefix, errorMessage);
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errorMessage);
            }

            if (log.DateCompleted.HasValue)
            {
                errorMessage = "Schedule was previously completed.";
                Trace.TraceWarning("{0}{1}", prefix, errorMessage);
                return Request.CreateErrorResponse(HttpStatusCode.Conflict, errorMessage);
                
            }

            if (!string.IsNullOrEmpty(log.MachineQueuedOn))
            {
                if (!log.MachineQueuedOn.ToLower().Equals(model.MachineQueuedOn.ToLower()))
                {
                    errorMessage = "Schedule already assigned to another processing agent.";
                    Trace.TraceWarning("{0}{1}", prefix, errorMessage);
                    return Request.CreateErrorResponse(HttpStatusCode.Conflict, errorMessage);
                }
            }
                        
            if (model.IsComplete)
            {
                Trace.TraceInformation("{0}{1}", prefix, "Schedule marked as complete");

                log.Fusion.ForceRefresh = false;
                log.DateCompleted = DateTime.UtcNow;
            }
            log.MachineQueuedOn = model.MachineQueuedOn;
            log.Message = model.Message;
            if (model.Success)
            {
                log.Success = model.Success;
            }

            Company.Update<FusionStatusLog>(log);

            Trace.TraceInformation("{0}{1}", prefix, "Schedule updated");

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// Internal endpoint.
        /// </summary>
        /// <param name="typeID">The ID of the fusion type.</param>
        /// <param name="fusionID">The ID of the fusion configuration.</param>
        /// <returns></returns>
        [Route("{typeID:int}/configurations/{fusionID:int}/attributes")]
        public List<FusionAttributeItem> GetAttributesByFusion(int typeID, int fusionID)
        {
            return Company.GetAttributesByFusion(fusionID);
        }
    }
}
