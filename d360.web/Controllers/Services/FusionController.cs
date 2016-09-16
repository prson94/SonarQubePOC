using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Xml.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using d360.core.entities.api;
using System.Text.RegularExpressions;
using d360.core.exceptions;
using d360.core.entities.Views;
using d360.fusion;
using SpreadsheetLight;
using System.IO;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling third-party metadata synchronization.
    /// </summary>
    [RoutePrefix("services/fusion"), Authorize, Name("Fusion Service")]
    public class FusionController : BaseApiController
    {
        #region DI

        IStorageProvider Storage;
        IQueueSource Queue;

        public FusionController(CommunityContext community, CompanyContext company, IStorageProvider storage, IQueueSource queue)
            : base(community, company)
        {
            Storage = storage;
            Queue = queue;
        }

        #endregion

        private static Regex _invalidXMLChars = new Regex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled);

        [Route("")]
        public IQueryable<FusionType> GetTypes()
        {
            return Company.Table<FusionType>();
        }

        [Route("{id:int}/attributetypes")]
        public IQueryable<FusionAttributeType> GetFusionAttributeTypes(int id)
        {
            return Company.Filter<FusionAttributeType>(i => i.FusionTypeID == id).AsQueryable();
        }

        [Route("attributetypes")]
        public IQueryable<dynamic> GetFusionAttributeTypes()
        {
            return Company.FusionAttributeTypes.Select(i =>
            new {
                ID = i.ID,
                ParentID = i.ParentID,
                TextPath = i.TextPath,
                Name = i.Name
            }).AsQueryable();
        }

        /// <summary>
        /// Get all available fusion configurations accross all types.  These configurations provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>A list of available fusion configurations.</returns>
        [Route("configurations")]
        public IQueryable<dynamic> GetConfigurations()
        {
            return Company.Filter<Fusion>(i => 1==1, i => i.FusionType)
                .Select(i => new {
                    i.Name, 
                    i.Description, 
                    i.FusionTypeID, 
                    FusionType = i.FusionType.Name, 
                    i.ID, 
                    i.Enabled
                });
        }

        /// <summary>
        /// Get all available fusion configurations accross all types.  These configurations provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>A list of available fusion configurations.</returns>
        [Route("configurations/excel.xls")]
        public HttpResponseMessage GetConfigurationsToExcel()
        {
            var results = Company.Filter<Fusion>(i => 1 == 1, i => i.FusionType)
                .Select(i => new {
                    i.Name,
                    i.Description,
                    i.FusionTypeID,
                    FusionType = i.FusionType.Name,
                    i.ID,
                    i.Enabled
                });

            var document = new SLDocument();
            document.AddWorksheet("Items");


            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Type");
            document.SetCellValue(1, 2, "Name");
            document.SetCellValue(1, 3, "Description");
            document.SetCellValue(1, 4, "Enabled");

            #endregion

            int r = 1;
            foreach (var row in results)
            {
                r++;
                document.SetCellValue(r, 1, row.FusionType);
                document.SetCellValue(r, 2, row.Name);
                document.SetCellValue(r, 3, row.Description);
                document.SetCellValue(r, 4, row.Enabled.ToString());
            }


            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            
            stream.Position = 0;

            HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK);
            //  result.
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Fusion configurations as of {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }
        
    

    /// <summary>
    /// Get all available fusion configurations for a specific type.  These configurations provide required connection and security credentials to connect to the underlying source.
    /// </summary>
    /// <returns>A list of available fusion configurations.</returns>
        [Route("{id:int}/configurations")]
        public HttpResponseMessage GetConfigurationsByType(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the fusion configuration details.");

            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Fusion", out joins, out columns);

            var querySql = string.Format(@"select	A.ID,
		A.Name,
		A.Description,
        A.FusionTypeID,
		T.Name as FusionType,
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
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the fusion configuration details.");

            var model = Company.GetFusionAsDictionary(id);
            if (model == null) return Request.CreateResponse(HttpStatusCode.NotFound);
            return Request.CreateResponse<Dictionary<string, object>>(HttpStatusCode.OK, model);
        }

        /// <summary>
        /// Get a specific fusion configuration.  This configuration will provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>The specific configuration.</returns>
        [Route("configurationById/{id:int}")]
        public HttpResponseMessage GetConfigurationById(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the fusion configuration details.");

            var model = Company.GetFusionAsDictionary(id);
            if (model == null) return Request.CreateResponse(HttpStatusCode.NotFound);
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

        #region History/Errors/Results

        /// <summary>
        /// Contains information relating to a specific instance the Fusion Agent is scheduled to run, as well as wether it completed successfully.
        /// </summary>
        [DataContract(Name = "AgentHistory", Namespace = constants.NAMESPACE)]
        public class AgentHistoryApiModel
        {
            /// <summary>
            /// Date that the scheduler create this run record.  This corresponds roughly to when a local Fusion Agent picks up the scheduled task.
            /// </summary>
            [DataMember]
            public DateTime DateStarted { get; set; }

            /// <summary>
            /// The date the Fusion Agent completes the task.
            /// </summary>
            [DataMember]
            public DateTime? DateCompleted { get; set; }

            /// <summary>
            /// The machine that contains the Fusion Agent that picked up the task.  The Fusion Agent sets this when picking up this task to effectively lock it so that no other Fusion Agents will act upon it.
            /// </summary>
            [DataMember]
            public string MachineQueuedOn { get; set; }

            /// <summary>
            /// A flag indicating wether the Fusion Agent successfully executing all steps for this task.
            /// </summary>
            [DataMember]
            public bool Success { get; set; }

            /// <summary>
            /// An optional message that the Fusion Agent can send when completing this task.  This usually contains an error message.
            /// </summary>
            [DataMember]
            public string Message { get; set; }

            /// <summary>
            /// The ID of the particular Fusion instance/configuration that this task references.
            /// </summary>
            [DataMember]
            public int FusionID { get; set; }

            /// <summary>
            /// The Name of the particular Fusion instance/configuration that this task references.
            /// </summary>
            [DataMember]
            public string Fusion { get; set; }

            /// <summary>
            /// The ID of the particular Fusion instance's type that this task references.
            /// </summary>
            [DataMember]
            public int FusionTypeID { get; set; }

            /// <summary>
            /// The Name of the particular Fusion instance's type that this task references.
            /// </summary>
            [DataMember]
            public string FusionType { get; set; }
        }
        
        /// <summary>
        /// You may optionally perform OData queries on this URI, such as:
        ///  - $filter
        ///  - $orderby
        ///  - $skip
        ///  - $take
        ///  For example, [URI]?$filter=FusionID%20eq%201&$orderby=DateStarted&$skip=10&$take=10
        /// </summary>
        /// <returns>Returns a list of history records.</returns>
        [Route("agenthistory")]
        public IQueryable<AgentHistoryApiModel> GetAgentHistory()
        {
            return Company.Query<AgentHistoryApiModel>(QueryConstants.AgentHistoryList).AsQueryable();
        }

        public class ExecutionHistoryApiModel
        {
            public int ID { get; set; }
            public string RawLogFileName { get; set; }
            public DateTime DateStarted { get; set; }
            public DateTime? DateCompleted { get; set; }
            public int Adds { get; set; }
            public int Updates { get; set; }
            public int Deletes { get; set; }
            public int ErrorCount { get; set; }
            public int ResultCount { get; set; }
            public int FusionID { get; set; }
            public string Fusion { get; set; }
            public int FusionTypeID { get; set; }
            public string FusionType { get; set; }
        }

        [Route("executionhistory")]
        public IQueryable<ExecutionHistoryApiModel> GetExecutionHistory()
        {
            return Company.Query<ExecutionHistoryApiModel>(QueryConstants.ExecutionHistoryList).AsQueryable();
        }

        public class ExecutionErrorModel
        {
            public DateTime Date { get; set; }
            public string Error { get; set; }
            public int ExecutionID { get; set; }
            public int FusionID { get; set; }
            public string Fusion { get; set; }
            public int FusionTypeID { get; set; }
            public string FusionType { get; set; }
        }

        [Route("executionerrors")]
        public IQueryable<ExecutionErrorModel> GetExecutionErrors()
        {
            return Company.Query<ExecutionErrorModel>(QueryConstants.ExecutionErrorList).AsQueryable();
        }

        public class AgentErrorModel
        {
            public DateTime Date { get; set; }
            public string Message { get; set; }
            public string MachineName { get; set; }
            public int FusionID { get; set; }
            public string Fusion { get; set; }            
            public string FusionType { get; set; }
        }

        [Route("agenterrors")]
        public IQueryable<AgentErrorModel> GetAgentErrors()
        {
            return Company.Query<AgentErrorModel>(QueryConstants.AgentErrorList).AsQueryable();
        }

        [Route("executions/{id:int}/results")]
        public HttpResponseMessage GetExecutionResults(int id) 
        {
            var querySql = string.Format(QueryConstants.ExecutionResultList, id);

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            countSql = applyFilteringSuffix(countSql, Request);
            int total = Company.Query<int>(countSql).First();

            sql = applyFilteringSuffix(sql, Request);
            sql = applySortSuffix(sql, Request, "FusionAttribute");
            sql = applyPagingSuffix(sql, Request);

            var query = Company.Query<FusionExecutionResultDetail>(sql);

            return Request.CreateResponse(HttpStatusCode.OK, new { total, results = query });
            
            //return Company.Query<FusionExecutionResultDetail>(sql, new { id = id }).AsQueryable();
        }

        #endregion

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

            //model.IsComplete = log.DateCompleted.HasValue;
            if (model.IsComplete)
            {
                Trace.TraceInformation("{0}{1}", prefix, "Schedule marked as complete");

                log.Fusion.ForceRefresh = false;
                log.DateCompleted = DateTime.UtcNow;
            }
            log.MachineQueuedOn = model.MachineQueuedOn;
            log.Message = model.Message;
            if (model.Success) log.Success = model.Success;

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

        /// <summary>
        /// Takes a given set of fusion data for a particular fusion configuration.
        /// </summary>
        /// <param name="typeID">The ID of the fusion type.</param>
        /// <param name="fusionID">The fusion configuration</param>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("{typeID:int}/configurations/{fusionID:int}/attributes")]
        public async Task<HttpResponseMessage> PostBulkAttributesAsync(int typeID, int fusionID)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to upload fusion data for this configuration.");

            var prefix = "Fusion.PostBulkAttributesAsync => ";
            var errorMessage = "";

            if (!Request.Content.IsMimeMultipartContent())
            {
                Trace.TraceWarning("{0}{1}", prefix, "Payload must be multipart content.");
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var streamProvider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(streamProvider);

            string json = await streamProvider.Contents.Single().ReadAsStringAsync();
            var import = JsonConvert.DeserializeObject<BulkFusionImport>(json);

            var date = DateTime.UtcNow;
            var fileName = "";
            try
            {
                var folder = string.Format("bulk-fusion-{0}", Company.CurrentCompanyID);
                Storage.CreateFolder(folder);
                fileName = string.Format("{0}.{1}.{2}.json", typeID, fusionID, date.ToString("yyyy-MM-dd_hh.mm.ss"));
                Storage.CreateFile(folder, fileName, json);
                Trace.TraceInformation("{0}{1}", prefix, "Saved raw json data to storage container.");

                Trace.TraceInformation("Enqueueing new fusion job on the queue.  Fusion ID: {0}, Company ID: {1}, Log:{2}",fusionID,Company.CurrentCompanyID, fileName);

                //get the name of the fusion queue from the database server name
                var db = Community.Query<DatabaseServer>( @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",new { id = Company.CurrentCompanyID }).SingleOrDefault();

                Trace.TraceInformation("Fusion queue name is: {0}, Company ID: {1}", db.FusionQueue, Company.CurrentCompanyID);

                var fusionQueue = new FusionQueueManager(db.FusionQueue);

                await fusionQueue.SendMessageAsync(new FusionProcessingData
                {
                    CompanyID = Company.CurrentCompanyID,
                    FusionID = fusionID,
                    LogFileName = fileName
                });

                //await Task.WhenAll(task);

                Trace.TraceInformation("Done enqueueing the fusion job on the queue.");

                //check if the job encountered any errors.  If so log the agent errors in fusion agenterror tables
                if(import.Errors != null && import.Errors.Count > 0)
                {
                    var host = "";
                    //agent encountered errors log them for display in the ui
                    if (System.Web.HttpContext.Current != null)
                        host = System.Web.HttpContext.Current.Request.UserHostName;


                    FusionAgentError error = new FusionAgentError
                    {
                        Date = DateTime.Now,
                        FusionID = fusionID,
                        MachineName = host
                    };

                    Company.Add<FusionAgentError>(error);


                    error.FusionAgentErrorItems = new List<FusionAgentErrorItem>();
                    
                    foreach(var item in import.Errors)
                    {
                        error.FusionAgentErrorItems.Add(new FusionAgentErrorItem
                        {
                            Message = item,
                            AgentErrorID = error.ID,
                            Date = DateTime.Now
                        });
                    }

                    if(error.FusionAgentErrorItems.Count > 0)
                    {                         
                        Company.Update<FusionAgentError>(error);
                    }
                }

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);
            }

            json = null;

            return Request.CreateResponse<string>(HttpStatusCode.OK, "Now parsing items");
        }

        /// <summary>
        /// Gets whether the agent should upload its log data.
        /// </summary>
        /// <returns>Http Status Code: 202 (Accepted), 204 (No Content)</returns>
        [Route("log"), HttpGet]
        public HttpResponseMessage GetAgentLogRequestStatus()
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see if the agent log should be uploaded.");

            var company = Community.GetById<Company>(Community.CurrentCompanyID);
            var synch = false;
            if (company != null)
            {
                synch = company.SynchAgentLog;
            }
            if (synch)
            {
                company.SynchAgentLog = false;
                Community.Update<Company>(company);
                return Request.CreateResponse(HttpStatusCode.Accepted);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }

        /// <summary>
        /// Takes a given agent log and saves it to long-term storage for analysis.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("log")]
        public async Task<HttpResponseMessage> PostAgentLogAsync()
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to upload this agent log.");

            var prefix = "Fusion.PostAgentLogAsync => ";

            if (!Request.Content.IsMimeMultipartContent())
            {
                Trace.TraceWarning("{0}{1}", prefix, "Payload must be multipart content.");
                return Request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, "Payload must be multipart content.");
            }

            try
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(streamProvider);

                string json = await streamProvider.Contents.Single().ReadAsStringAsync();

                var date = DateTime.UtcNow;

                var folder = string.Format("agent-log-{0}", Company.CurrentCompanyID);
                Storage.CreateFolder(folder);
                Storage.CreateFile(folder, string.Format("{0}.json", date.ToString("yyyy-MM-dd_hh.mm.ss")), json);
                
                Trace.TraceInformation("{0}{1}", prefix, "Saved raw json data to storage container.");

                json = null;

                return Request.CreateResponse<string>(HttpStatusCode.OK, "Saved agent log");
            }
            catch (Exception ex)
            {
                SendException(ex, new Dictionary<string, string>());
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(), ex);
            }
        }

        [HttpGet, Route("list")]
        public List<StorageFileInfo> GetList(int typeId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            var company = Community.GetById<Company>(Community.CurrentCompanyID);

            var container = constants.AZURE_CLOUD_FUSION_CONTAINER;

            Storage.CreateFolder(container);
            var folder = string.Format("{2}/{0}.{1}", Company.CurrentCompanyID,typeId, constants.AZURE_CLOUD_FUSION_CONTAINER);
            
            return Storage.ListFiles(folder);            
        }

        [HttpPost, Route("uploadfile")]
        public async Task<IHttpActionResult> PostUploadFile()
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            var prefix = "Fusion.PostUploadFile => ";

            Trace.TraceInformation("{0}{1}", prefix, "Starting to upload cloud fusion files.");

            try {                
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                var typeid = Request.Headers.GetValues("fusionTypeId").FirstOrDefault();

                foreach (var file in provider.Contents)
                {
                    if (file.IsFormData() || string.IsNullOrEmpty(file.Headers.ContentDisposition.FileName)) continue;
                    var filename = file.Headers.ContentDisposition.FileName.Trim('\"');
                    filename = filename.Replace('\\', '/').ToLower();
                    var contents = await file.ReadAsStringAsync();
                    //prepend the company id and fusion type to the file name
                    string newFileName = string.Format("{0}.{1}/{2}", Company.CurrentCompanyID, typeid, filename);
                    Trace.TraceInformation("{0}{1}{2}", prefix, "Saving To Cloud fusion... ", newFileName);
                    Storage.CreateFile(constants.AZURE_CLOUD_FUSION_CONTAINER, newFileName, contents);
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.GetFullExceptionData());

                var msg = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                msg.ReasonPhrase = ex.InnerException != null ? ex.InnerException.Message.Replace(@"\n", "; ") : ex.Message.Replace(@"\n", "; ");
                throw new HttpResponseException(msg);
            }

            Trace.TraceInformation("{0}{1}", prefix, "Completed upload of cloud fusion files.");

            return Ok();
        }

        public class PromotionHistoryApiModel
        {
            public int ID { get; set; }            
            public DateTime DateStarted { get; set; }
            public DateTime? DateCompleted { get; set; }
            public int PromotedTaxonomies { get; set; }
            public int PromotedDomainItems { get; set; }
            public int PromotedDomains { get; set; }
            public int PromotedArtifacts { get; set; }
            public int TotalNewPromotions { get; set; }
            public int AttributesConsidered { get; set; }
            public string NumberOfRules { get; set; }
            public int RelationshipsAdded { get; set; }
        }

        [Route("promotionhistory")]
        public IQueryable<PromotionHistoryApiModel> GetPromotionHistory()
        {
            return Company.Query<PromotionHistoryApiModel>(QueryConstants.PromotionHistoryList).AsQueryable();
        }


        [Route("promotions/{typeID:int}")]
        public IQueryable<dynamic> GetPromotionsByAttributeType(int typeID)
        {
            var sql = @"select 
	                        s.id, r.Description + ' - ' + s.[Description] as Name, t.Name as AttributeName, t.TextPath, t.ID as AttributeID, t.ParentID 
                        from 
	                        fusion.rulestep s
                        join fusion.[rule] r on r.id = s.ruleid
                        join fusionattributetype t on t.id = r.objectid
                        where s.Action = 'Promote'
                        and r.objectid = @typeID";

            return Company.Query<dynamic>(sql, new { typeID }).AsQueryable();
        }

        [Route("rules/parent/{ruleID:int}")]
        public IQueryable<FusionAttributeType> GetParentTypeByRule(int ruleID)
        {
            var sql = @"
                    select t2.* from fusion.[rule] r
                    join fusionattributetype t on t.id = r.objectid
                    join fusionattributetype t2 on t2.id = t.parentid
                    where r.id = @ruleID";

            return Company.Query<FusionAttributeType>(sql, new { ruleID }).AsQueryable();
        }
    }
}
