using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Web.Http;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;

namespace d360.web.Controllers.Services
{
    [ApiVersion("1.0"), RoutePrefix("services/events"), Authorize]
    public class EventsController : BaseApiController
    {
        TelemetryClient Telemetry;

        #region DI

        public EventsController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
            Telemetry = new TelemetryClient();
            Telemetry.Context.InstrumentationKey = ConfigurationManager.AppSettings["AppInsightsInstrumentationKey"];
            Telemetry.Context.Properties["CompanyID"] = company.CurrentCompanyID.ToString();
        }

        #endregion

        #region Models

        [DataContract]
        public class ResultQualifierModel
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string Value { get; set; }

            public string ResultObject { get; set; }

            public int? ResultObjectID { get; set; }
        }

        [DataContract]
        public class RuleImplementationModel
        {
            [DataMember]
            public string SourceID { get; set; }
            [DataMember]
            public string SourceUri { get; set; }
            [DataMember]
            public string Name { get; set; }
        }

        [DataContract]
        public class ResultModel
        {
            [DataMember]
            public DateTime EffectiveDate { get; set; }
            [DataMember]
            public DateTime RunDate { get; set; }
            [DataMember]
            public int RowsPassed { get; set; }
            [DataMember]
            public int RowsFailed { get; set; }
            [DataMember]
            public int? FusionID { get; set; }
            [DataMember]
            public List<string> FusionAttributes { get; set; }
            [DataMember]
            public List<ResultQualifierModel> Qualifiers { get; set; }

            /// <summary>
            /// Implementation SourceID, if there is one.
            /// </summary>
            [DataMember]
            public string SourceID { get; set; }

            /// <summary>
            /// Used internally.
            /// </summary>
            public string QualifierHash { get; set; }

            /// <summary>
            /// Used internally.
            /// </summary>
            public int? RuleImplementationID { get; set; }
        }

        #endregion
                
        /// <summary>
        /// Gets an OData-queryable list of rules contained within your environment.
        /// </summary>
        /// <returns>A list of rules.</returns>
        [Route("rules"), HttpGet]
        public IQueryable<Rule> GetRules()
        {
            return Company.Table<Rule>();
        }
                
        /// <summary>
        /// Add a rule to your environment.  Once created, this rule can hold events
        /// </summary>
        /// <param name="id">The rule Type ID</param>
        /// <param name="model">A rule</param>
        /// <returns>Http Status. 401:Unauthorized, 404:NotFound, 201:Created.  If 201, the new rule is also returned.</returns>
        [Route("rules/{id:int}"), HttpPost]
        public HttpResponseMessage AddRule(int id, RuleModel model)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.RuleType, id, Permission.ModifyAsset))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add a rule.");

            Rule item = null;

            try
            {
                var type = Company.GetById<RuleType>(id);

                // Check that RuleType was found
                if (type == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Rule type with ID of ({id}) not found.");

                // Check that the dimension was found.
                if (string.IsNullOrEmpty(model.RuleDimension))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Rule dimension not provided.");

                // Check that the status was found.
                if ((int)model.Status <= 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Rule status not found.");

                var dimension = Company.Filter<RuleDimension>(i => i.Name.ToLower() == model.RuleDimension.Trim().ToLower()).FirstOrDefault();
                if (dimension == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Rule dimension with name of ({model.RuleDimension}) not found.");


                if (!string.IsNullOrEmpty(model.SourceID))
                {
                    item = Company.Filter<Rule>(i => i.SourceID.ToLower() == model.SourceID.Trim().ToLower()).FirstOrDefault();
                }

                var exists = false;

                if (item != null)
                {
                    exists = true;

                    item.SourceID = model.SourceID;
                    item.Threshold = (model.Threshold.HasValue) ? model.Threshold.Value : 0.90M;
                    item.RuleDimensionID = dimension.ID;
                }
                else
                {
                    item = new Rule
                    {
                        Threshold = (model.Threshold.HasValue) ? model.Threshold.Value : 0.90M,
                        RuleTypeID = id,
                        Status = model.Status,
                        SourceID = model.SourceID,
                        RuleDimensionID = dimension.ID
                    };
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.RuleType, id).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.Fields.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Rule.ToString(), Value = model.Fields[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Rule");
                    }
                });

                Company.SaveOrUpdate(item, fields);


                if (exists)
                    return Request.CreateResponse(HttpStatusCode.OK, item);
                else
                    return Request.CreateResponse(HttpStatusCode.Created, item);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        /// <summary>
        /// Update a specific rule.
        /// </summary>
        /// <param name="typeID">The type ID</param>
        /// <param name="id">The rule ID</param>
        /// <param name="model">The rule fields</param>
        /// <returns>Artifact</returns>
        [Route("rules/{typeID:int}/{id:int}"), HttpPut]
        public HttpResponseMessage EditRule(int typeID, int id, RuleModel model)
        {
            try
            {
                if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this rule.");

                var item = Company.GetById<Rule>(id);

                if (item == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
                else
                {
                    if (item.RuleTypeID != typeID)
                    {
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                    }
                }

                // Check that the dimension was found.
                var dimension = Company.Filter<RuleDimension>(i => i.Name.ToLower() == model.RuleDimension.Trim().ToLower()).FirstOrDefault();
                if (dimension == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Rule dimension with name of ({model.RuleDimension}) not found.");
                }


                item.Threshold = (model.Threshold.HasValue) ? model.Threshold.Value : 0.90M;
                item.Status = item.Status;
                item.RuleDimensionID = dimension.ID;

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.RuleType, typeID).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.Fields.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Rule.ToString(), ObjectID = item.ID, Value = model.Fields[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                });

                Company.SaveOrUpdate<Rule>(item, fields);

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }


        /// <summary>
        /// Add one or more events to a rule.
        /// </summary>
        /// <param name="sourceID">The underlying source ID of the system the the rule originated from.</param>
        /// <param name="models">A collection of aggregated rule results.</param>
        /// <returns></returns>
        [
            Route("sourcerules/{sourceID}/events"),
            Route("sourcerules/{sourceID}/results"),
            HttpPost
        ]
        public HttpResponseMessage AddSourceRuleEvents(string sourceID, List<ResultModel> models)
        {
            var rule = Company.Filter<RuleImplementation>(m => m.SourceID == sourceID).Select(m => m.Rule).FirstOrDefault();
            if (rule != null)
            {
                return AddRuleResults(rule.ID, models);
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Rule could not be located based on the Source ID: {sourceID}.");
            }
        }

        internal class ImplementationQualifier
        {
            public int RuleID { get; set; }
            public int? ImplementationID { get; set; }
            public string SourceID { get; set; }
            public string Name { get; set; }
            public int? Order { get; set; }
            public int? RuleResultQualifierTypeID { get; set; }
        }

        /// <summary>
        /// Add one or more events to a rule.
        /// </summary>
        /// <param name="id">The ID of the rule to add events to.</param>
        /// <param name="models">A collection of aggregated rule results.</param>
        /// <returns></returns>
        [
            Route("rules/{id:int}/results"),
            Route("rules/{id:int}/events"),
            HttpPost
        ]
        public HttpResponseMessage AddRuleResults(int id, List<ResultModel> models)
        {
            if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add results to this rule.");

            if (models == null)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = "AddRuleResults => No Models Found", SeverityLevel = SeverityLevel.Error });

                var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
                msg.ReasonPhrase = "Request body is invalid.  Please reformat your request.";
                throw new HttpResponseException(msg);
            }
            else
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = Newtonsoft.Json.JsonConvert.SerializeObject(models), SeverityLevel = SeverityLevel.Information });
            }

            var implementationQualifiers = Company.Query<ImplementationQualifier>(@"
select	R.ID as RuleID,
		I.ID as ImplementationID,
		I.SourceID,
		QT.ID as RuleResultQualifierTypeID,
        QT.Name,
		QT.[Order]
from	[Rule] R
        left join [RuleImplementation] I on I.RuleID = R.ID
		left join [RuleResultQualifierType] QT on QT.RuleImplementationID = I.ID 
where	R.ID = @id
order by I.ID, QT.Name", new { id }).ToList();

            var hashImplementations = new Dictionary<int, string>();
            var hasher = SHA1.Create();

            #region Calculate Hashes for Implementations and Qualifier Names

            var uniqueImplementationIDs = implementationQualifiers.Where(i => i.ImplementationID.HasValue).Select(i => i.ImplementationID.Value).Distinct().ToList();
            uniqueImplementationIDs.ForEach(i =>
            {
                var qualNames = string.Join("|", implementationQualifiers.Where(o => o.ImplementationID == i).OrderBy(o => o.Name).Select(o => o.Name));
                byte[] hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(qualNames));
                var sb = new StringBuilder();
                foreach (byte bt in hashBytes)
                {
                    sb.Append(bt.ToString("x2"));
                }
                hashImplementations.Add(i, sb.ToString());
            });

            #endregion

            #region Calculate Hashes for Model Qualifier Names

            models.ForEach(m =>
            {
                if (m.Qualifiers != null)
                {
                    var qualNames = string.Join("|", m.Qualifiers.Select(o => o.Name).OrderBy(o => o));
                    byte[] hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(qualNames));
                    var sb = new StringBuilder();
                    foreach (byte bt in hashBytes)
                    {
                        sb.Append(bt.ToString("x2"));
                    }
                    m.QualifierHash = sb.ToString();
                }
            });

            #endregion

            var errorList = new List<CreateResponse>();

            try
            {
                var loop = 1;
                foreach (var model in models)
                {
                    try
                    {
                        #region Try to identify or create the RuleImplementation record.

                        //First match on qualifier hash.
                        if (hashImplementations.ContainsValue(model.QualifierHash))
                        {
                            model.RuleImplementationID = hashImplementations.Single(v => v.Value == model.QualifierHash).Key;
                        }
                        else
                        {
                            //If qualifier hashes do not line up, then see if we can match on incoming implementation sourceID.
                            if (!string.IsNullOrEmpty(model.SourceID))
                            {
                                var iq = implementationQualifiers.FirstOrDefault(i => i.SourceID.Trim() == model.SourceID.Trim());
                                if (iq != null)
                                {
                                    model.RuleImplementationID = iq.ImplementationID.Value;
                                }
                            }

                            //If still no value, then assume this is a new implementation to add.
                            if (!model.RuleImplementationID.HasValue)
                            {
                                var ri = new RuleImplementation { RuleID = id, SourceID = model.SourceID };
                                Company.Add(ri);



                                model.RuleImplementationID = ri.ID;

                                var position = 1;
                                model.Qualifiers.ForEach(o =>
                                {
                                    var q = new RuleResultQualifierType { RuleImplementationID = ri.ID, Name = o.Name, Order = position };
                                    Company.Add(q);

                                    //add to collection we are using in this execution.
                                    implementationQualifiers.Add(new ImplementationQualifier { ImplementationID = ri.ID, Name = o.Name, Order = position, RuleID = id, RuleResultQualifierTypeID = q.ID, SourceID = ri.SourceID });

                                    //increment position.
                                    position++;
                                });

                                //Save this implementation in the hash comparison list.
                                if (model.Qualifiers != null) { 
                                    var qualNames = string.Join("|", model.Qualifiers.OrderBy(o => o.Name).Select(o => o.Name));
                                    byte[] hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(qualNames));
                                    var sb = new StringBuilder();
                                    foreach (byte bt in hashBytes)
                                    {
                                        sb.Append(bt.ToString("x2"));
                                    }

                                    hashImplementations.Add(ri.ID, sb.ToString());
                                }
                            }
                        }

                        #endregion

                        if (model.RuleImplementationID.HasValue)
                        {
                            var result = new RuleResult { EffectiveDate = model.EffectiveDate, RunDate = model.RunDate, RowsFailed = model.RowsFailed, RowsPassed = model.RowsPassed, RuleImplementationID = model.RuleImplementationID.Value };

                            if (model.FusionAttributes == null)
                                model.FusionAttributes = new List<string>();
                            model.FusionAttributes.ForEach(a =>
                            {
                                if (result.RuleResultFusionAttributes == null)
                                    result.RuleResultFusionAttributes = new List<RuleResultFusionAttribute>();

                                result.RuleResultFusionAttributes.Add(new RuleResultFusionAttribute { FusionAttribute = a });
                            });

                            if (model.Qualifiers == null)
                                model.Qualifiers = new List<ResultQualifierModel>();
                            model.Qualifiers.ForEach(q =>
                            {
                                var qt = implementationQualifiers.Single(i => i.ImplementationID == model.RuleImplementationID && i.Name == q.Name);

                                if (result.RuleResultQualifiers == null)
                                    result.RuleResultQualifiers = new List<RuleResultQualifier>();

                                result.RuleResultQualifiers.Add(new RuleResultQualifier {
                                    RuleResultQualifierTypeID = qt.RuleResultQualifierTypeID.Value,
                                    Value = q.Value,
                                    CreatedBy = Company.CurrentResourceID,
                                    UpdatedBy = Company.CurrentResourceID,
                                    CreatedOn = DateTime.UtcNow,
                                    UpdatedOn = null //ensures the rule processor picks this up at least once
                                });
                            });

                            Company.RuleResults.Add(result);
                        }
                        else
                        {
                            errorList.Add(new CreateResponse { Message = $"Row {loop} has no valid rule implementation ID." });
                        }

                    }
                    catch (Exception ex)
                    {
                        Telemetry.TrackTrace(new TraceTelemetry { Message = $"AddRuleResults => {ex.GetFullExceptionData()}", SeverityLevel = SeverityLevel.Critical });
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.GetFullExceptionData(), ex);
                    }

                    loop++;
                }
                // Save the results.
                Company.SaveChanges();

                return Request.CreateResponse((errorList.Count == 0) ? HttpStatusCode.Created : HttpStatusCode.BadRequest, errorList);
            }
            catch (BaseException ex)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"AddRuleResults => {ex.GetFullExceptionData()}", SeverityLevel = SeverityLevel.Critical });
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"AddRuleResults => {ex.GetFullExceptionData()}", SeverityLevel = SeverityLevel.Critical });
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        /// <summary>
        /// Add one or more results to a rule implementation.
        /// </summary>
        /// <param name="id">The ID of the rule to add results to.</param>
        /// <param name="implementationID">The ID of the rule implementation to add results to.</param>
        /// <param name="models">A collection of aggregated rule results.</param>
        /// <returns></returns>
        [
            Route("rules/{id:int}/{implementationID:int}/results"),
            Route("rules/{id:int}/{implementationID:int}/events"),
            HttpPost
        ]
        public HttpResponseMessage AddRuleImplementationResults(int id, int implementationID, List<ResultModel> models)
        {
            if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add results to this rule implementation.");

            if (models == null)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = "AddRuleImplementationResults => No Models Found", SeverityLevel = SeverityLevel.Error });

                var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
                msg.ReasonPhrase = "Request body is invalid.  Please reformat your request.";
                throw new HttpResponseException(msg);
            }
            else
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = Newtonsoft.Json.JsonConvert.SerializeObject(models), SeverityLevel = SeverityLevel.Information });
            }

            var implementationQualifiers = Company.Query<ImplementationQualifier>(@"
select	I.RuleID,
		I.ID as ImplementationID,
		I.SourceID,
		QT.ID as RuleResultQualifierTypeID,
        QT.Name,
		QT.[Order]
from	RuleImplementation I
		left join RuleResultQualifierType QT on QT.RuleImplementationID = I.ID 
where	I.ID = @id
order by I.ID, QT.Name", new { id = implementationID }).ToList();

            var hashImplementations = new Dictionary<int, string>();
            var hasher = SHA1.Create();

            #region Calculate Hashes for Implementations and Qualifier Names

            var uniqueImplementationIDs = implementationQualifiers.Where(i => i.ImplementationID.HasValue).Select(i => i.ImplementationID.Value).Distinct().ToList();
            uniqueImplementationIDs.ForEach(i =>
            {
                var qualNames = string.Join("|", implementationQualifiers.Where(o => o.ImplementationID == i).OrderBy(o => o.Name).Select(o => o.Name));
                byte[] hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(qualNames));
                var sb = new StringBuilder();
                foreach (byte bt in hashBytes)
                {
                    sb.Append(bt.ToString("x2"));
                }
                hashImplementations.Add(i, sb.ToString());
            });

            #endregion

            #region Calculate Hashes for Model Qualifier Names

            models.ForEach(m =>
            {
                var qualNames = string.Join("|", m.Qualifiers.Select(o => o.Name).OrderBy(o => o));
                byte[] hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(qualNames));
                var sb = new StringBuilder();
                foreach (byte bt in hashBytes)
                {
                    sb.Append(bt.ToString("x2"));
                }
                m.QualifierHash = sb.ToString();
            });

            #endregion

            var errorList = new List<CreateResponse>();

            try
            {
                var loop = 1;
                foreach (var model in models)
                {
                    try
                    {
                        #region Try to identify or create the RuleImplementation record.

                        //First match on qualifier hash.
                        if (hashImplementations.ContainsValue(model.QualifierHash))
                        {
                            model.RuleImplementationID = hashImplementations.Single(v => v.Value == model.QualifierHash).Key;
                        }

                        #endregion

                        if (model.RuleImplementationID.HasValue)
                        {
                            var result = new RuleResult { EffectiveDate = model.EffectiveDate, RunDate = model.RunDate, RowsFailed = model.RowsFailed, RowsPassed = model.RowsPassed, RuleImplementationID = model.RuleImplementationID.Value };

                            model.FusionAttributes.ForEach(a =>
                            {
                                if (result.RuleResultFusionAttributes == null)
                                    result.RuleResultFusionAttributes = new List<RuleResultFusionAttribute>();

                                result.RuleResultFusionAttributes.Add(new RuleResultFusionAttribute { FusionAttribute = a });
                            });

                            model.Qualifiers.ForEach(q =>
                            {
                                var qt = implementationQualifiers.Single(i => i.ImplementationID == model.RuleImplementationID && i.Name == q.Name);

                                if (result.RuleResultQualifiers == null)
                                    result.RuleResultQualifiers = new List<RuleResultQualifier>();

                                result.RuleResultQualifiers.Add(new RuleResultQualifier {
                                    RuleResultQualifierTypeID = qt.RuleResultQualifierTypeID.Value,
                                    Value = q.Value,
                                    CreatedBy = Company.CurrentResourceID,
                                    UpdatedBy = Company.CurrentResourceID,
                                    CreatedOn = DateTime.UtcNow,
                                    UpdatedOn = null //ensures the rule processor picks this up at least once
                                });
                            });

                            Company.RuleResults.Add(result);
                        }
                        else
                        {
                            errorList.Add(new CreateResponse { Message = $"Row {loop} has no valid rule implementation ID." });
                        }

                    }
                    catch (Exception ex)
                    {
                        Telemetry.TrackTrace(new TraceTelemetry { Message = $"AddRuleImplementationResults => {ex.GetFullExceptionData()}", SeverityLevel = SeverityLevel.Critical });
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.GetFullExceptionData(), ex);
                    }

                    loop++;
                }
                // Save the results.
                Company.SaveChanges();

                return Request.CreateResponse((errorList.Count == 0) ? HttpStatusCode.Created : HttpStatusCode.BadRequest, errorList);
            }
            catch (BaseException ex)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"AddRuleResults => {ex.GetFullExceptionData()}", SeverityLevel = SeverityLevel.Critical });
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                Telemetry.TrackTrace(new TraceTelemetry { Message = $"AddRuleResults => {ex.GetFullExceptionData()}", SeverityLevel = SeverityLevel.Critical });
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }


        /// <summary>
        /// Gets all results for a rule across all implementations.
        /// </summary>
        /// <param name="id">The ID of the rule to get results from.</param>
        /// <returns></returns>
        [
            Route("rules/{id:int}/events"),
            Route("rules/{id:int}/results"),
            HttpGet
        ]
        public HttpResponseMessage GetRuleResults(int id)
        {
            var sql = $@"
select	A.RuleImplementationID,
		RI.Name as RuleImplementation,
		A.ID,
        A.RowsPassed,
        A.RowsFailed,
        A.PassFraction,
        A.FailFraction,
        A.Passed,
        A.EffectiveDate,
        A.RunDate,
		F.FusionAttributes,
		Q.Qualifiers
from	RuleResult A  
		inner join RuleImplementation RI on RI.ID = A.RuleImplementationID and RI.RuleID = @id
		cross apply (
					select	STRING_AGG(RQT.Name + ': ' + RQ.Value, ', ') as Qualifiers
					from	RuleResultQualifier RQ
							inner join RuleResultQualifierType RQT on RQT.ID = RQ.RuleResultQualifierTypeID and RQT.RuleImplementationID = RI.ID and RQ.RuleResultID = A.ID
					) Q
		cross apply (
					select	STRING_AGG(COALESCE(FA.TextPath, RF.FusionAttribute), ', ') as FusionAttributes
					from	RuleResultFusionAttribute RF
							left join FusionAttribute FA on FA.ID = RF.FusionAttributeID and RF.RuleResultID = A.ID
					) F
order by A.RunDate desc, A.EffectiveDate desc";

            var models = Company.Query<dynamic>(sql, new { id });

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, models);
        }


        /// <summary>
        /// Gets all results for a rule implementation.
        /// </summary>
        /// <param name="id">The ID of the rule.</param>
        /// <param name="implementationID">The ID of the rule imeplementation to get results from.</param>
        /// <returns></returns>
        [
            Route("rules/{id:int}/implementations/{implementationID:int}/events"),
            Route("rules/{id:int}/implementations/{implementationID:int}/results"),
            HttpGet
        ]
        public HttpResponseMessage GetRuleImplementationResults(int id, int implementationID)
        {
            var joins = "";
            var columns = "";

            var fields = Company.Filter<RuleResultQualifierType>(i => i.RuleImplementationID == implementationID).OrderBy(i => i.Order).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");
                columns += $"{name}_T.Value as [{name}], ";
                joins += $" left join RuleResultQualifier {name}_T on {name}_T.RuleResultID = A.ID and {name}_T.RuleResultQualifierTypeID = {f.ID}";
            }


            var sql = $@"
select	A.ID,
        A.RowsPassed,
        A.RowsFailed,
        A.PassFraction,
        A.FailFraction,
        A.Passed,
        A.EffectiveDate,
        A.RunDate,
        {columns}
		F.FusionAttributes
from	RuleResult A  
        {joins} 
		cross apply (
					select	STRING_AGG(COALESCE(FA.TextPath, RF.FusionAttribute), ', ') as FusionAttributes
					from	RuleResultFusionAttribute RF
							left join FusionAttribute FA on FA.ID = RF.FusionAttributeID and RF.RuleResultID = A.ID
					) F
where   A.RuleImplementationID = @implementationID
order by A.RunDate desc, A.EffectiveDate desc";

            var models = Company.Query<dynamic>(sql, new { implementationID });

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, models);
        }

        /// <summary>
        /// Add a rule implementation to your environment.  Once created, this rule can hold results
        /// </summary>
        /// <param name="id">The rule ID</param>
        /// <param name="model">A rule implemetation</param>
        /// <returns>Http Status. 401:Unauthorized, 404:NotFound, 201:Created.  If 201, the new rule is also returned.</returns>
        [Route("implementation/{id:int}"), HttpPost]
        public HttpResponseMessage AddRuleImplementation(int id, RuleImplementationModel model)
        {
            if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add a rule implementation.");

            RuleImplementation impl = new RuleImplementation();

            try
            {
                var rule = Company.GetById<Rule>(id);

                // Check that rule was found
                if (rule == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                impl.RuleID = rule.ID;
                impl.SourceID = model.SourceID;
                impl.SourceUri = model.SourceUri;
                impl.Name = model.Name;
                
                Company.Add<RuleImplementation>(impl);

                Company.SaveChanges();
                
                return Request.CreateResponse(HttpStatusCode.Created, impl);
            }        
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        /// <summary>
        /// Add Qualifies against provided ruleImplementation ID
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Route("ruleimplementation/qualifier"), HttpPost]
        public HttpResponseMessage AddQualifier(RuleResultQualifierType model)
        {
            try
            {
                #region verify model and its value
                // Check that model was found
                if (model == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Model is null.");
                }
                // Check that ruleimplementatioID is present or not
                if (model.RuleImplementationID <= 0 )
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Rule Implementation ID is not provided.");
                }

                if (string.IsNullOrEmpty(model.ResolutionObject)
                    || string.IsNullOrEmpty(model.ResolutionFieldTypeName))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, @"ResolutionObject and ResolutionFieldTypeName can not be blank.");
                }
                if (model.ResolutionObjectID <= 0
                    || model.ResolutionFieldTypeID <= 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, @"ResolutionObjectID and ResolutionFieldTypeID should be greater than 0.");
                }

                if (!QualifierResolutionObjectsExits(model.ResolutionObjectID.Value, model.ResolutionObject))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, @"Values of ResolutionObjectID and ResolutionObject are not valid");
                }

                if (!FieldTypesByObject((SystemObjects)Enum.Parse(typeof(SystemObjects), model.ResolutionObject), model.ResolutionObjectID.Value, model.ResolutionFieldTypeID.Value, model.ResolutionFieldTypeName))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, @"Values of ResolutionFieldTypeID and ResolutionFieldTypeName are not valid");
                }
                #endregion

                #region verify rule implementation exists
                var ruleImplementation = Company.GetById<RuleImplementation>(model.RuleImplementationID);

                // Check that rule implementation was found or not
                if (ruleImplementation == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Rule Implementation is not found against provided ID :"+model.RuleImplementationID);
                }
                #endregion

                #region save model to table
                model.Order = Company.Count<RuleResultQualifierType>(r => r.RuleImplementationID == model.RuleImplementationID) + 1;

                Company.RuleResultQualifierTypes.Add(model);
                Company.SaveChanges();
                #endregion

                return Request.CreateResponse(HttpStatusCode.Created, model);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        public bool QualifierResolutionObjectsExits(int id, string type)
        {
           return Company.Query<dynamic>(string.Format(@"select ID, [Type], [value], [label] from (
                        select ID, 'ArtifactType' as [Type],  'ArtifactType|' + cast(ID as varchar(50)) as [value],  'Artifact :: ' + [Name] as [label] from ArtifactType
                        union all
                        select ID, 'TaxonomyType' as [Type], 'TaxonomyType|' + cast(ID as varchar(50)) as [value],  'Model :: ' + [Name] as [label] from TaxonomyType
                        union all
                        select ID, 'ReferenceItemType' as [Type], 'ReferenceItemType|' + cast(ID as varchar(50)) as [value], 'Reference :: ' + [Name] as [label] from ReferenceItemType
				        ) as t
				        where t.ID = {0} and  t.[Type] = '{1}'",id, type)).Count() > 0;
        }

        public bool FieldTypesByObject(SystemObjects type, int id, int fieldId, string fieldName)
        {
            return Company
                .GetFieldTypesByObject(type, id)
                .Where(i => i.ID == fieldId && i.FriendlyName == fieldName).Count() > 0;
        }


    }
}
