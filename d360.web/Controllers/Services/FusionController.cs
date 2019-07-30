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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Linq;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling third-party metadata synchronization.
    /// </summary>
    [ApiVersion("1.0"), RoutePrefix("services/fusion"), Authorize, Name("Fusion Service"), ApiExplorerSettings(IgnoreApi = true)]
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
            return Company.Table<FusionType>();
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
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the fusion configuration details.");

            var model = Company.GetFusionAsDictionary(id);
            if (model == null) return Request.CreateResponse(HttpStatusCode.NotFound);
            return Request.CreateResponse<Dictionary<string, object>>(HttpStatusCode.OK, model);
        }

        [Route("{typeID:int}/configurations/{id:int}/attributetypes")]
        public IEnumerable<FusionAttributeTypeWithQuery> GetFusionAttributeTypes(int typeID, int id)
        {
            return Company.Query<FusionAttributeTypeWithQuery>(@"
                    select	A.ID,
                            A.Name,
                            A.ScanEnabled,
                            Q.Query
                    from    Fusion C
                            inner join FusionAttributeType A on A.FusionTypeID = C.FusionTypeID and C.ID = @id
                            left join FusionAttributeTypeCustomQuery Q on Q.FusionAttributeTypeID = A.ID and Q.FusionID = C.ID", 
                                new { id }
                            );
            
        }

        /// <summary>
        /// Get a specific fusion configuration's query attribute types.  This list will provide required SQL statement to execute against the underlying relational source.
        /// </summary>
        /// <returns>The specific configuration's query attribute types.</returns>
        [Route("{typeID:int}/configurations/{id:int}/queries")]
        public HttpResponseMessage GetConfigurationQueries(int typeID, int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the fusion configuration details.");

            try
            {
                var keyFields = Company.Query<FusionQueryAttributeTypeKeyField>("select Q.ID, F.Name from FusionQueryAttributeType Q inner join FieldType F on F.Object = 'FusionQueryAttributeType' and F.ObjectID = Q.ID and F.IsPartOfKey = 1 and Q.FusionID = @id", new { id });

                var models = Company.Filter<FusionQueryAttributeType>(i => i.FusionID == id)
                    .ToList()
                    .Select(i => new FusionQueryAttributeTypeApiModel
                    {
                        ID = i.ID,
                        KeyColumns = keyFields.Where(f => f.ID == i.ID).Select(f => f.Name).ToList(),
                        Name = i.Name,
                        Query = i.Query
                    });

                if (models == null) return Request.CreateResponse(HttpStatusCode.NotFound);
                return Request.CreateResponse(HttpStatusCode.OK, models);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get a specific fusion configuration.  This configuration will provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>The specific configuration.</returns>
        [Route("configurationById/{id:int}")]
        public HttpResponseMessage GetConfigurationById(int id)
        {
            var model = Company.GetFusionAsDictionary(id);
            if (model == null) return Request.CreateResponse(HttpStatusCode.NotFound);

            var asset = Company.Assets.Where(x => x.Object == "Fusion" && x.ObjectID == id).FirstOrDefault();

            if(asset!= null)
            {
                model["AssetID"] = asset.ID;
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
        /// Get all available fusion configurations for a specific type.  These configurations provide required connection and security credentials to connect to the underlying source.
        /// </summary>
        /// <returns>A list of available fusion configurations.</returns>
        [Route("{fusionTypeID:int}/configurations/{fusionID:int}/schedules")]
        public IQueryable<FusionSchedule> GetConfigurationsByType(int fusionTypeID, int fusionID)
        {
            if (Company.CurrentResourceIsAdmin)
            {                
                return Company.Filter<FusionSchedule>(i => i.FusionID == fusionID);
            }
            else
            {
                return null;
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
        /// </summary>
        /// <returns>Returns a list of history records.</returns>
        [Route("agenthistory")]
        public IQueryable<AgentHistoryApiModel> GetAgentHistory()
        {
            return Company.Query<AgentHistoryApiModel>(QueryConstants.AgentHistoryList).AsQueryable();
        }

        [Route("agenthistoryexport")]
        public HttpResponseMessage GetAgentHistoryExport(int top = 100, int fusionId = -1)
        {
            var sql = string.Format(QueryConstants.AgentHistoryExportList, top);

            if (fusionId > 0)
                sql += $" where S.fusionId = {fusionId}";

            sql += " order by S.DateStarted desc";

            var res = Company.Query<AgentHistoryApiModel>(sql);

            var document = new SLDocument();
            document.AddWorksheet("Items");


            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Type");
            document.SetCellValue(1, 2, "Configuration");
            document.SetCellValue(1, 3, "Started");
            document.SetCellValue(1, 4, "Completed");
            document.SetCellValue(1, 5, "Success");
            
            #endregion

            int r = 1;
            foreach (var row in res)
            {
                r++;
                document.SetCellValue(r, 1, row.FusionType);
                document.SetCellValue(r, 2, row.Fusion);
                document.SetCellValue(r, 3, row.DateStarted.ToLocalTime().ToString());

                if(row.DateCompleted.HasValue) document.SetCellValue(r, 4, row.DateCompleted.GetValueOrDefault().ToLocalTime().ToString());
                document.SetCellValue(r, 5, row.Success);
            }


            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);

            stream.Position = 0;

            HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK);            
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Fusion agent history as of {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
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

        [Route("executionhistoryexport")]        
        public HttpResponseMessage GetExecutionHistoryExport(int top = 100, int fusionId = -1)
        {
            var sql = string.Format(QueryConstants.ExecutionHistoryExportList, top);

            if (fusionId > 0)
                sql += $" where E.fusionId = {fusionId}";

            sql += " order by DateStarted desc";

            var res = Company.Query<ExecutionHistoryApiModel>(sql);

            var document = new SLDocument();
            document.AddWorksheet("Items");
            
            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Type");
            document.SetCellValue(1, 2, "Configuration");
            document.SetCellValue(1, 3, "Started");
            document.SetCellValue(1, 4, "Completed");
            document.SetCellValue(1, 5, "Errors");
            document.SetCellValue(1, 6, "Results");
            document.SetCellValue(1, 7, "Adds");
            document.SetCellValue(1, 8, "Deletes");
            document.SetCellValue(1, 9, "Updates");
            document.SetCellValue(1, 10, "Data File");

            #endregion

            int r = 1;
            foreach (var row in res)
            {
                r++;
                document.SetCellValue(r, 1, row.FusionType);
                document.SetCellValue(r, 2, row.Fusion);
                document.SetCellValue(r, 3, row.DateStarted.ToLocalTime().ToString());

                if (row.DateCompleted.HasValue) document.SetCellValue(r, 4, row.DateCompleted.GetValueOrDefault().ToLocalTime().ToString());
                document.SetCellValue(r, 5, row.ErrorCount);
                document.SetCellValue(r, 6, row.ResultCount);
                document.SetCellValue(r, 7, row.Adds);
                document.SetCellValue(r, 8, row.Deletes);
                document.SetCellValue(r, 9, row.Updates);
                document.SetCellValue(r, 10, row.RawLogFileName);
            }
            
            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);

            stream.Position = 0;

            HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK);            
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Fusion execution history as of {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
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


        [Route("executionerrorsexport/{executionId:int}")]
        public HttpResponseMessage GetExecutionErrorsExport(int executionId)
        {
            var sql = string.Format(QueryConstants.ExecutionErrorExportList, executionId);
            var res = Company.Query<ExecutionErrorModel>(sql);
            
            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Date");
            document.SetCellValue(1, 2, "Error");
            
            #endregion

            int r = 1;
            foreach (var row in res)
            {
                r++;
                document.SetCellValue(r, 1, row.Date.ToLocalTime().ToString());
                document.SetCellValue(r, 2, row.Error);            
            }


            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);

            stream.Position = 0;

            HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK);            
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Fusion execution errors {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
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

        [Route("executions/{id:int}/exportresults")]
        public HttpResponseMessage GetExecutionResultsExport(int id)
        {
            var dbArgs = new Dapper.DynamicParameters();
            var querySql = string.Format(QueryConstants.ExecutionResultList, id);

            var filter = Request.GetQueryString("filter");

            if (!string.IsNullOrEmpty(filter))
            {
                querySql += $" and (A.TextPath like @filter or AT.TextPath like @filter or E.FieldName like @filter or E.OldValue like @filter or E.NewValue like @filter)";

                // add value to db args
                dbArgs.Add("filter", $"{filter}%", System.Data.DbType.AnsiString, System.Data.ParameterDirection.Input, 200);

            }
                        
            var sql = string.Format(@"select * from ({0}) A", querySql);
            
            sql = applySortSuffix(sql, Request, "FusionAttribute");            

            var query = Company.Query<FusionExecutionResultDetail>(sql, dbArgs);

            var document = new SLDocument();
            document.AddWorksheet("Items");
            
            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Type");
            document.SetCellValue(1, 2, "Attribute");
            document.SetCellValue(1, 3, "Action");
            document.SetCellValue(1, 4, "Field");
            document.SetCellValue(1, 5, "Old Value");
            document.SetCellValue(1, 6, "New Value");

            #endregion

            int r = 1;
            foreach (var row in query)
            {
                r++;
                document.SetCellValue(r, 1, row.FusionAttributeType);
                document.SetCellValue(r, 2, row.FusionAttribute);
                document.SetCellValue(r, 3, row.Action);
                document.SetCellValue(r, 4, row.FieldName);
                document.SetCellValue(r, 5, row.OldValue);
                document.SetCellValue(r, 6, row.NewValue);
            }


            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);

            stream.Position = 0;

            HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK);            
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Fusion execution results {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }

        [Route("executions/{id:int}/results")]
        public HttpResponseMessage GetExecutionResults(int id) 
        {
            var dbArgs = new Dapper.DynamicParameters();

            var querySql = string.Format(QueryConstants.ExecutionResultList, id);

            var filter = Request.GetQueryString("filter");

            if(!string.IsNullOrEmpty(filter))
            {
                querySql += $" and (A.TextPath like @filter or AT.TextPath like @filter or E.FieldName like @filter or E.OldValue like @filter or E.NewValue like @filter)";
                
                // add value to db args
                dbArgs.Add("filter", $"{filter}%", System.Data.DbType.AnsiString, System.Data.ParameterDirection.Input, 200);

            }

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);

            var sql = string.Format(@"select * from ({0}) A", querySql);
            
            countSql = applyFilteringSuffix(countSql, Request);
            int total = Company.Query<int>(countSql, dbArgs).First();
                        
            sql = applySortSuffix(sql, Request, "FusionAttribute");
            sql = applyPagingSuffix(sql, Request);

            var query = Company.Query<FusionExecutionResultDetail>(sql, dbArgs);

            return Request.CreateResponse(HttpStatusCode.OK, new { total, results = query });            
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
        /// Gets all overrides queries for a given fusion attribute type within a specific fusion configuration.
        /// </summary>
        /// <returns>A list of available overrides for a configuration.</returns>
        [Route("{fusionTypeID:int}/configurations/{fusionID:int}/queryoverrides")]
        public HttpResponseMessage GetQueryOverridesByConfiguration(int fusionTypeID, int fusionID)
        {
            if (Company.CurrentResourceIsAdmin)
            {
                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    Company.Filter<FusionAttributeTypeCustomQuery>(i => i.FusionID == fusionID, i => i.FusionAttributeType).Select(o => new {
                        FusionAttributeType = o.FusionAttributeType.TextPath,
                        o.FusionAttributeTypeID,
                        o.FusionID,
                        o.ID,
                        o.Query
                    })
                );
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Internal endpoint.
        /// </summary>
        /// <param name="typeID">The ID of the fusion type.</param>
        /// <param name="fusionID">The ID of the fusion configuration.</param>
        /// <returns></returns>
        [Route("{typeID:int}/configurations/{fusionID:int}/queryattributetypes")]
        public IQueryable<FusionQueryAttributeType> GetQueryAttributesByFusion(int typeID, int fusionID)
        {
            return Company.Filter<FusionQueryAttributeType>(i => i.FusionID == fusionID);
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

            #region Validation
            var fusion = Company.Filter<Fusion>(x => x.ID == fusionID).Select(x=>new { x.ID,x.FusionTypeID}).SingleOrDefault();
            if (fusion == null)
            {
                Trace.TraceWarning($"fusionID {fusionID} not found");
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"fusionID {fusionID} not found");
            }
            if (fusion.FusionTypeID != typeID)
            {
                Trace.TraceWarning($"typeID {typeID} doesn't match fusion {fusionID}");
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"typeID {typeID} doesn't match fusion {fusionID}");
            }
            #endregion

            var prefix = "Fusion.PostBulkAttributesAsync => ";
            var errorMessage = "";

            string json = "{}";

            if (Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(streamProvider);

                json = await streamProvider.Contents.Single().ReadAsStringAsync();
            }
            else
            {
                json = await Request.Content.ReadAsStringAsync();
            }

            var import = JsonConvert.DeserializeObject<BulkFusionImport>(json);

            var fileName = "";
            try
            {
                var folder = $"bulk-fusion-{Company.CurrentCompanyID}";
                Storage.CreateFolder(folder);
                fileName = $"{typeID}.{fusionID}.{DateTime.UtcNow.ToString("yyyy-MM-dd_hh.mm.ss")}.json";
                Storage.CreateFile(folder, fileName, json);

                Trace.TraceInformation("{0}{1}", prefix, "Saved raw json data to storage container.");

                Trace.TraceInformation("Enqueueing new fusion job on the queue.  Fusion ID: {0}, Company ID: {1}, Log:{2}",fusionID,Company.CurrentCompanyID, fileName);

                await Queue.CreateMessageAsync(Config.GetValue<string>("FusionLoadQueue"), new FusionProcessingData
                {
                    CompanyID = Company.CurrentCompanyID,
                    FusionID = fusionID,
                    LogFileName = fileName
                });

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

                    Company.Add(error);


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
                        Company.Update(error);
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

        [Route("{typeID:int}/configurations/{id:int}/template/{attributeTypeID:int}"), HttpPost]
        public async Task<IHttpActionResult> UploadFusionManualLoad(int typeID, int id, int attributeTypeID)
        {
            var context = Request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
            try
            {
                for (var indx = 0; indx < context.Request.Files.Count; indx++)
                {
                    var file = context.Request.Files[indx];
                    var fileExt = Path.GetExtension(file.FileName);
                    var target = new MemoryStream();
                    file.InputStream.CopyTo(target);

                    var xls = new SLDocument(target);
                    var stats = xls.GetWorksheetStatistics();
                    var endRowIndex = stats.EndRowIndex;
                    var currentRowIndex = 0;
                    var currentRowNumber = 1;

                    var fusion = Company.GetById<Fusion>(id, i => i.FusionType.FusionAttributeTypes);

                    var mXml = new XElement("ms");

                    var targetAttributeType = fusion.FusionType.FusionAttributeTypes.SingleOrDefault(i => i.ID == attributeTypeID);
                    var targetAttributeTypeFields = Company.Filter<FieldType>(i => i.Object == "FusionAttributeType" && i.ObjectID == attributeTypeID).ToList();

                    if (targetAttributeType != null)
                    {
                        var parentAttributeTypeIDs = new List<int>();
                        int? parentID = targetAttributeType.ParentID;

                        #region Determine the correct order of the fusion attribute IDs (1. Schema / 2. Table / 3. Column)

                        while (parentID.HasValue)
                        {
                            var parent = fusion.FusionType.FusionAttributeTypes.SingleOrDefault(i => i.ID == parentID.Value);
                            if (parent != null)
                            {
                                parentAttributeTypeIDs.Insert(0, parent.ID);
                                parentID = parent.ParentID;
                            }
                        }

                        #endregion

                        var currentColumnIndex = 1;

                        var models = new List<Dictionary<string, string>>();

                        #region Parse raw ancestor nodes.

                        foreach (var nodeID in parentAttributeTypeIDs)
                        {
                            currentRowIndex = 2;    // Reset the current row index.
                            var sourceIDs = new List<string>();

                            while (currentRowIndex <= endRowIndex)
                            {

                                #region Create SourceID

                                var sourceID = "";
                                for (int i = 1; i <= currentColumnIndex; i++)
                                {
                                    sourceID += ((sourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                                }

                                #endregion

                                //Check to see if we already added this.
                                if (!sourceIDs.Any(i => i == sourceID))
                                {
                                    sourceIDs.Add(sourceID);

                                    #region Create ParentSourceID

                                    var parentSourceID = "";
                                    for (int i = 1; i < currentColumnIndex; i++)
                                    {
                                        parentSourceID += ((parentSourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                                    }

                                    #endregion

                                    var jsonFields = new Dictionary<string, string>();

                                    jsonFields.Add("Name", xls.GetCellValueAsString(currentRowIndex, currentColumnIndex));
                                    jsonFields.Add("SourceID", sourceID);
                                    jsonFields.Add("FusionAttributeTypeID", nodeID.ToString());
                                    if (!string.IsNullOrEmpty(parentSourceID))
                                    {
                                        jsonFields.Add("ParentSourceID", parentSourceID);
                                    }

                                    models.Add(jsonFields);


                                    currentRowNumber++;    // This number must be unique.  A unique number per fusion attribute.                            
                                }

                                currentRowIndex++;
                            }

                            currentColumnIndex++;
                        }

                        #endregion

                        #region Get the target fusion attribute type rows

                        #region import File Record Column Validation
                        var colIndex = 1;
                        foreach (var nodeID in parentAttributeTypeIDs)
                        {
                            var t = fusion.FusionType.FusionAttributeTypes.Single(i => i.ID == nodeID);

                            if (t.Name.Trim().ToLower() != xls.GetCellValueAsString(1, colIndex).Trim().ToLower())
                            {
                                var errMsg = string.Format("{0} could not be uploaded because a required column is missing or incorrect.", file.FileName);
                                throw new InvalidFieldException(errMsg);
                            }
                            colIndex++;
                        }

                        if (targetAttributeType.Name.Trim().ToLower() != xls.GetCellValueAsString(1, colIndex).Trim().ToLower())
                        {
                            var errMsg = string.Format("{0} could not be uploaded because a required column is missing or incorrect.", file.FileName);
                            throw new InvalidFieldException(errMsg);
                        }
                        colIndex++;
                        for (int i = 0; i < targetAttributeTypeFields.Count; i++)
                        {
                            if (targetAttributeTypeFields[i].Name.Trim().ToLower() != xls.GetCellValueAsString(1, colIndex).Trim().ToLower())
                            {
                                var errMsg = string.Format("{0} could not be uploaded because a required column is missing or incorrect.", file.FileName);
                                throw new InvalidFieldException(errMsg);

                            }
                            colIndex++;
                        }
                        #endregion  
                        currentRowIndex = 2;    // Reset the current row index.
                        while (currentRowIndex <= endRowIndex)
                        {
                            #region Create SourceID

                            var sourceID = "";
                            for (int i = 1; i <= currentColumnIndex; i++)
                            {
                                sourceID += ((sourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                            }

                            #endregion

                            #region Create ParentSourceID

                            var parentSourceID = "";
                            for (int i = 1; i < currentColumnIndex; i++)
                            {
                                parentSourceID += ((parentSourceID != "") ? "." : "") + xls.GetCellValueAsString(currentRowIndex, i).ToLower();
                            }

                            #endregion

                            var jsonFields = new Dictionary<string, string>();

                            jsonFields.Add("Name", xls.GetCellValueAsString(currentRowIndex, currentColumnIndex));
                            jsonFields.Add("SourceID", sourceID);
                            jsonFields.Add("FusionAttributeTypeID", attributeTypeID.ToString());
                            if (!string.IsNullOrEmpty(parentSourceID))
                            {
                                jsonFields.Add("ParentSourceID", parentSourceID);
                            }

                            for (int i = 0; i < targetAttributeTypeFields.Count; i++)
                            {
                                jsonFields.Add(targetAttributeTypeFields[i].Name, xls.GetCellValueAsString(currentRowIndex, i + currentColumnIndex + 1));
                            }

                            models.Add(jsonFields);

                            currentRowNumber++;    // This number must be unique.  A unique number per fusion attribute.
                            currentRowIndex++;
                        }

                        #endregion

                        #region Save to queue for processing

                        var import = new BulkFusionImport { Models = models, Relationships = new FusionRelationshipModels() };

                        var json = JsonConvert.SerializeObject(import);

                        var folder = $"bulk-fusion-{Company.CurrentCompanyID}";
                        Storage.CreateFolder(folder);
                        var fileName = $"{typeID}.{id}.{DateTime.UtcNow.ToString("yyyy-MM-dd_hh.mm.ss")}.json";
                        Storage.CreateFile(folder, fileName, json);

                        await Queue.CreateMessageAsync(Config.GetValue<string>("FusionLoadQueue"), new FusionProcessingData
                        {
                            CompanyID = Company.CurrentCompanyID,
                            FusionID = id,
                            LogFileName = fileName
                        });

                        #endregion
                    }
                }

                return await Task.FromResult(successMessageResponse(HttpStatusCode.OK, "File Saved", "File uploaded and queued for processing."));
            }
            catch (InvalidFieldException ex)
            {
                return await Task.FromResult(errorMessageResponse(ex.StatusCode, "Invalid Field", ex.StatusDescription));
            }
            catch (BaseException ex)
            {
                return await Task.FromResult(errorMessageResponse(ex.StatusCode, "Govern Exception", ex.StatusDescription));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown Error", ex.Message));
            }
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

                Storage.CreateFile("agent-log", $"{Company.CurrentCompanyID}/{date.ToString("yyyy-MM-dd_hh.mm.ss")}.json", json);
                
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

        [Route("{fusionID:int}/{fusionQueryAttributeTypeID:int}/data")]
        public HttpResponseMessage GetFusionQueryAttributesByFusionAndType(int fusionID, int fusionQueryAttributeTypeID, bool? metadata = false)
        {
            HttpResponseMessage response = null;

            try
            {
                var joins = "";
                var columns = "";

                var fields = Company.Filter<FieldType>(i => i.Object == "FusionQueryAttributeType" && i.ObjectID == fusionQueryAttributeTypeID).OrderBy(i => i.SortOrder).ToList();

                foreach (var f in fields)
                {
                    var tableName = $"Field{f.ID}";
                    columns += ((string.IsNullOrEmpty(columns)) ? "" : ", ") + $"{tableName}_T.FormattedValue as [{f.Name}]";
                    joins += $@" left join FieldWithRelation {tableName}_T on {tableName}_T.ObjectType = 'FusionQueryAttribute' and {tableName}_T.ObjectID = A.ID and {tableName}_T.FieldTypeID = {f.ID} ";
                }

                if (columns.Contains("[type]"))
                    columns = columns.Replace("[type]", "[_type]");

                var dbArgs = new Dapper.DynamicParameters();
                dbArgs.Add("f", fusionID);
                dbArgs.Add("t", fusionQueryAttributeTypeID);

                #region Query

                var sql = $@"
select  {columns} 
from	FusionQueryAttribute A 
        inner join FusionQueryAttributeType T on T.ID = A.FusionQueryAttributeTypeID and T.FusionID = @f and T.ID = @t 
        {joins} 
where   A.Deleted = 0";

                var models = Company.Query<dynamic>(sql, dbArgs);

                #endregion

                if (metadata.GetValueOrDefault())
                {
                    List<dynamic> header = new List<dynamic>();

                    var firstRow = models.FirstOrDefault();

                    if (firstRow != null)
                    {
                        foreach (KeyValuePair<string, object> kvp in firstRow)
                        { // enumerating over it exposes the Properties and Values as a KeyValuePair
                            var dataType = typeof(string).ToString();

                            if (kvp.Value != null)
                                dataType = kvp.Value.GetType().ToString();

                            header.Add(new { field = kvp.Key, type = dataType });
                        }
                    }

                    response = Request.CreateResponse(HttpStatusCode.OK, new { metadata = header, data = models });
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, models);
                }
            }
            catch (SqlException ex)
            {
                response = Request.CreateErrorResponse(HttpStatusCode.PreconditionFailed, ex.GetFullExceptionData(), ex);
            }

            return response;
        }

        [HttpGet, Route("list")]
        public List<StorageFileInfo> GetList(int typeId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;
                        
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

        public class RuleStepPromotionHistoryModel
        {
            public int ID { get; set; }
            public int AttributeID { get; set; }
            public string AttributeType { get; set; }
            public string AttributeName { get; set; }
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string ObjectName { get; set; }
            public string ObjectUrl { get; set; }
            public DateTime CreatedOn { get; set; }
            public DateTime UpdatedOn { get; set; }
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
    }
}
