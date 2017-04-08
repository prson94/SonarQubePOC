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

        //public class CreateEventsModelRequest
        //{
        //    public CreateEventsModelRequest()
        //    {
        //        Events = new List<CreateEventModelRequest>();
        //    }

        //    public string GroupKey { get; set; }
        //    public int? EventCount { get; set; }
        //    public string Name { get; set; }

        //    public List<CreateEventModelRequest> Events { get; set; }
        //}

        public class ResultQualifierModel
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string ResultObject { get; set; }
            public int? ResultObjectID { get; set; }
        }

        public class ResultModel
        {
            public DateTime EffectiveDate { get; set; }
            public DateTime RunDate { get; set; }
            public int RowsPassed { get; set; }
            public int RowsFailed { get; set; }
            public int? FusionID { get; set; }
            public List<string> FusionAttributes { get; set; }
            public List<ResultQualifierModel> Qualifiers { get; set; }

            /// <summary>
            /// Implementation SourceID, if there is one.
            /// </summary>
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

        //public class CreateEventModelResponse
        //{
        //    public int ID { get; set; }
        //    public string SourceID { get; set; }
        //    public string ResponseCode { get; set; }
        //    public string ResponseMessage { get; set; }
        //}

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
        /// <param name="model">A policy</param>
        /// <returns>Http Status. 401:Unauthorized, 404:NotFound, 201:Created.  If 201, the new policy is also returned.</returns>
        [Route("policies"), HttpPost]
        public HttpResponseMessage AddPolicy(PolicyModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add a policy.");

            if (model.ParentID.HasValue)
            {
                if (Company.GetById<Policy>(model.ParentID.Value) == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("The Parent Policy does not exist for ID: {0}.", model.ParentID.Value));
            }

            try
            {
                var policy = new Policy
                {
                    Description = model.Description,
                    Name = model.Name,
                    ParentID = model.ParentID
                };

                Company.Add<Policy>(policy);
                return Request.CreateResponse<Policy>(HttpStatusCode.Created, policy);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

        }

        /// <summary>
        /// Add a rule to your environment.  Once created, this rule can hold events
        /// </summary>
        /// <param name="model">A rule</param>
        /// <returns>Http Status. 401:Unauthorized, 404:NotFound, 201:Created.  If 201, the new rule is also returned.</returns>
        [Route("rules"), HttpPost]
        public HttpResponseMessage AddRule(RuleModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add a rule.");

            try
            {
                if (model == null)
                {
                    throw new MissingPropertiesException("Rule");
                }

                Rule rule = null;

                if (!string.IsNullOrEmpty(model.SourceID))
                {
                    if (Company.Any<RuleImplementation>(i => i.SourceID == model.SourceID))
                    {
                        throw new ConflictException("Rule already exists", $"A rule with the source ID of {model.SourceID} already exists.");
                    }
                }

                rule = new Rule
                {
                    Description = model.Description,
                    Measurement = model.Measurement,
                    Purpose = model.Purpose,
                    Resolution = model.Resolution,
                    Threshold = (model.Threshold.HasValue) ? model.Threshold.Value : 0.90M,
                    Name = model.Name,
                    RuleType = model.RuleType,
                    Status = RuleStatus.Draft,
                    RuleDimensionID = model.RuleDimensionID
                };

                if (!string.IsNullOrEmpty(model.SourceID))
                {
                    rule.RuleImplementations = new List<RuleImplementation>();
                    rule.RuleImplementations.Add(new RuleImplementation { SourceID = model.SourceID });
                }

                Company.Add<Rule>(rule);
                return Request.CreateResponse<Rule>(HttpStatusCode.Created, rule);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
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
                            }
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

        ///// <summary>
        ///// Add one or more relationships to a rule based on the source ID from an underlying system that originally created the rule.
        ///// </summary>
        ///// <param name="sourceID">The underlying source ID of the system the the rule originated from.</param>
        ///// <param name="models">A collection of relationships.</param>
        ///// <returns></returns>
        //[Route("sourcerules/{sourceID}/relationships"), HttpPost]
        //public HttpResponseMessage AddSourceRuleRelationships(string sourceID, List<ObjectModel> models)
        //{
        //    var rule = Company.Filter<RuleMap>(m => m.SourceID == sourceID).Select(m => m.Rule).FirstOrDefault();
        //    if (rule != null)
        //    {
        //        return addRuleRelationships(rule.ID, models, rule);
        //    }
        //    else
        //    {
        //        return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = $"Rule could not be located based on the Source ID: {sourceID}." });
        //    }
        //}

        ///// <summary>
        ///// Add one or more relationships to a rule.
        ///// </summary>
        ///// <param name="id">The ID of the rule to add events to.</param>
        ///// <param name="models">A collection of relationships.</param>
        ///// <returns></returns>
        //[Route("rules/{id:int}/relationships"), HttpPost]
        //public HttpResponseMessage AddRuleRelationships(int id, List<ObjectModel> models)
        //{
        //    return addRuleRelationships(id, models);
        //}

        //HttpResponseMessage addRuleRelationships(int id, List<ObjectModel> models, Rule rule = null)
        //{
        //    try
        //    {
        //        if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update, ClaimObject.Root))
        //            return Request.CreateResponse(HttpStatusCode.Unauthorized, new { Message = "You are not allowed to add relationships to this rule." });

        //        if (models == null)
        //        {
        //            throw new MissingPropertiesException("Rule Relationships");
        //        }
        //        else
        //        {
        //            if (models.Count == 0)
        //            {
        //                throw new MissingPropertiesException("Rule Relationships");
        //            }
        //        }

        //        if (rule == null) //If no rule sent in, do a lookup.
        //        {
        //            rule = Company.GetById<Rule>(id);
        //        }

        //        if (rule == null)
        //        {
        //            throw new NotFoundException("Rule");
        //        }
        //        models.ForEach(m =>
        //        {
        //            var t = (SystemObjects)Enum.Parse(typeof(SystemObjects), m.ObjectType);
        //            Company.AddIntersect(SystemObjects.Rule, id, t, m.ObjectID, IntersectClassification.Normal, null, null);
        //        });
        //        //Company.AddRelationships(SystemObjects.Rule, id, IntersectClassification.Normal, null, null, models);

        //        return Request.CreateResponse(HttpStatusCode.Created, new { Message = "Relationships created." });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return Request.CreateResponse(ex.StatusCode, new { Message = ex.StatusMessage });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = $"An unknown error occured.  Please try again later. Error was: {ex.GetFullExceptionData()}" });
        //    }
        //}


//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="id">The ID of the rule to retrieve attributes for.</param>
//        /// <param name="typeID">The ID of the attribute type to get.</param>
//        /// <returns></returns>
//        [Route("rules/{id:int}/attributes/{typeID:int}"), HttpGet]
//        public HttpResponseMessage GetAttributesByAttributeType(int id, int typeID)
//        {
//            HttpResponseMessage response = null;

//            var joins = "";
//            var columns = "";
//            getDynamicFieldJoinStatements(typeID, "Attribute", out joins, out columns);

//            var querySql = string.Format(@"select A.ID, {0} T.Name
//from	Attribute A 
//inner join AttributeType T on T.ID = A.AttributeTypeID and T.ID = @typeID and A.ObjectType = 'Rule' and A.ObjectID = @id {1}", columns, joins);

//            var sql = string.Format(@"select * from ({0}) A", querySql);

//            var models = Company.Query<dynamic>(sql, new { id = id, typeID = typeID });
//            response = Request.CreateResponse(HttpStatusCode.OK, models);

//            return response;
//        }


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
    }
}
