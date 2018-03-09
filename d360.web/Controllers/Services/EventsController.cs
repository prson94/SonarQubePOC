using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
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
    [RoutePrefix("services/events"), Authorize]
    public class EventsController : BaseApiController
    {
        TelemetryClient Telemetry;

        #region DI

        public EventsController(CommunityContext community, CompanyContext company)
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
            public string SourceID { get; set; }
            public string SourceUri { get; set; }
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
        /// Gets an OData-queryable list of policies contained within your environment.
        /// </summary>
        /// <returns>A list of policies.</returns>
        [Route("policies"), HttpGet]
        public IQueryable<Policy> GetPolicies()
        {
            return Company.Table<Policy>();
        }

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
        /// Add a policy to your environment.  Once created, this policy can hold child policies and rules.
        /// </summary>
        /// <param name="id">The type ID</param>
        /// <param name="model">A policy</param>
        /// <returns>Http Status. 401:Unauthorized, 404:NotFound, 201:Created.  If 201, the new policy is also returned.</returns>
        [Route("policies/{id:int}"), HttpPost]
        public HttpResponseMessage AddPolicy(int id, Dictionary<string, string> model)
        {
            if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Create, ClaimObject.Root))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add a policy.");

            Policy item = null;

            try
            {
                var type = Company.GetById<PolicyType>(id);

                #region Check that PolicyType was found

                if (type == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                #endregion

                item.PolicyTypeID = id;

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("ParentID");
                    }
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.PolicyType, id).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Policy.ToString(), Value = model[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Policy");
                    }
                });

                Company.SaveOrUpdate<Policy>(item, fields);


                if (parentID > 0)
                {
                    var parent = Company.GetById<Policy>(parentID);
                    if (parent == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"The parent policy with id {parentID} could not be found.");
                    var intersectType = Company.GetHierarchyIntersectType(SystemObjects.PolicyType, parent.PolicyTypeID, id);
                    if (intersectType != null)
                    {
                        var intersect = new Intersect()
                        {
                            Subject = "Policy",
                            Object = "Policy",
                            SubjectID = parentID,
                            ObjectID = item.ID
                        };

                        Company.SaveOrUpdate(intersect);
                    }
                }


                return Request.CreateResponse<Policy>(HttpStatusCode.Created, item);
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
        /// Update a specific policy.
        /// </summary>
        /// <param name="typeID">The type ID</param>
        /// <param name="id">The policy ID</param>
        /// <param name="model">The policy fields</param>
        /// <returns>Artifact</returns>
        [Route("policies/{typeID:int}/{id:int}"), HttpPut]
        public HttpResponseMessage EditPolicy(int typeID, int id, Dictionary<string, string> model)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this policy.");

                var item = Company.GetById<Policy>(id);

                if (item == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
                else
                {
                    if (item.PolicyTypeID != typeID)
                    {
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                    }
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.PolicyType, typeID).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("ParentID");
                    }
                }

                if (parentID > 0)
                {
                    var parent = Company.GetById<Policy>(parentID);
                    var existing = Company.GetParentObject<Policy>(item.ID);
                    var intersectType = Company.GetHierarchyIntersectType(SystemObjects.TaxonomyType, parent.PolicyTypeID, item.PolicyTypeID);
                    if (intersectType == null)
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

                    if (existing == null)
                    {
                        var intersect = new Intersect()
                        {
                            Subject = "Policy",
                            Object = "Policy",
                            SubjectID = parentID,
                            ObjectID = item.ID,
                            IntersectTypeID = intersectType.ID,
                        };

                        Company.Add(intersect);
                    }
                    else if (existing.ID != parentID)
                    {
                        var intersect = Company.Filter<Intersect>(i => i.Subject == "Policy" && i.Object == "Policy" && i.SubjectID == existing.ID && i.ObjectID == item.ID).FirstOrDefault();
                        if (intersect != null)
                        {
                            intersect.SubjectID = parentID;
                            Company.Update(intersect);
                        }
                    }
                }

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Policy.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                });

                Company.SaveOrUpdate<Policy>(item, fields);

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
        /// Add a rule to your environment.  Once created, this rule can hold events
        /// </summary>
        /// <param name="id">The rule Type ID</param>
        /// <param name="model">A rule</param>
        /// <returns>Http Status. 401:Unauthorized, 404:NotFound, 201:Created.  If 201, the new rule is also returned.</returns>
        [Route("rules/{id:int}"), HttpPost]
        public HttpResponseMessage AddRule(int id, RuleModel model)
        {
            if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Create, ClaimObject.Root))
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
                if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update, ClaimObject.Root))
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
            if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update, ClaimObject.Root))
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

                                result.RuleResultQualifiers.Add(new RuleResultQualifier { RuleResultQualifierTypeID = qt.RuleResultQualifierTypeID.Value, Value = q.Value });
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
            if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update, ClaimObject.Root))
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

                                result.RuleResultQualifiers.Add(new RuleResultQualifier { RuleResultQualifierTypeID = qt.RuleResultQualifierTypeID.Value, Value = q.Value });
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
        /// Gets all results for a rule.
        /// </summary>
        /// <param name="id">The ID of the rule to get results from.</param>
        /// <returns></returns>
        [
            Route("rules/{id:int}/events"),
            Route("rules/{id:int}/results"),
            HttpGet
        ]
        public HttpResponseMessage GetRuleImplementationResults(int id)
        {
            var joins = "";
            var columns = "";

            var fields = Company.Filter<RuleResultQualifierType>(i => i.RuleImplementationID == id).OrderBy(i => i.Order).ToList();

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
        {columns}
        A.FusionAttributeID,
        --F.TextPath as FusionAttributePath,
        A.EffectiveDate,
        A.RunDate
from	RuleResult A  
        --left join FusionAttribute F on F.ID = A.FusionAttributeID
        {joins} 
where   A.RuleImplementationID = {id} 
order by A.RunDate desc, A.EffectiveDate desc";

            var models = Company.Query<dynamic>(sql);

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
            if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Create, ClaimObject.Root))
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
    }
}
