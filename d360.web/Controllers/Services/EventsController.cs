using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Query;
using System.Web.Http.OData;
using System.Dynamic;
using d360.core.exceptions;
using d360.core.enums;
using d360.web.Models.Attributes;
using d360.web.Models;

namespace d360.web.Controllers.Services
{
    [RoutePrefix("services/events"), Authorize]
    public class EventsController : BaseApiController
    {
        #region DI

        public EventsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
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
                    if (Company.Any<RuleMap>(i => i.SourceID == model.SourceID))
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
                    rule.Maps = new List<RuleMap>();
                    rule.Maps.Add(new RuleMap { SourceID = model.SourceID });
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
            var rule = Company.Filter<RuleMap>(m => m.SourceID == sourceID).Select(m => m.Rule).FirstOrDefault();
            if (rule != null)
            {
                return AddRuleResults(rule.ID, models);
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Rule could not be located based on the Source ID: {sourceID}.");
            }
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
                var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
                msg.ReasonPhrase = "Request body is invalid.  Please reformat your request.";
                throw new HttpResponseException(msg);
            }

            var rule = Company.GetById<Rule>(id, i => i.RuleResultQualifierTypes);

            var errorList = new List<CreateResponse>();

            try
            {
                var qualitifierTypes = rule.RuleResultQualifierTypes.ToList();

                var loop = 1;
                foreach (var model in models)
                {
                    try
                    {
                        var result = new RuleResult { EffectiveDate = model.EffectiveDate, RunDate = model.RunDate, RowsFailed = model.RowsFailed, RowsPassed = model.RowsPassed, RuleID = id };
                        var isResultValid = true;

                        model.Qualifiers.ForEach(q =>
                        {
                            var qt = qualitifierTypes.SingleOrDefault(i => i.Name == q.Name);
                            if (qt != null)
                            {
                                if (result.RuleResultQualifiers == null)
                                    result.RuleResultQualifiers = new List<RuleResultQualifier>();

                                result.RuleResultQualifiers.Add(new RuleResultQualifier { RuleResultQualifierTypeID = qt.ID, Value = q.Value });
                            }
                            else
                            {
                                isResultValid = false;
                            }
                        });

                        if (isResultValid)
                            Company.RuleResults.Add(result);
                        else
                            errorList.Add(new CreateResponse { Message = $"Row {loop} contains qualifiers that are not yet defined on the rule." });
                    }
                    catch (Exception ex)
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.GetFullExceptionData(), ex);
                    }

                    loop++;
                }
                // Save the results.
                Company.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.Created, errorList);
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
        public HttpResponseMessage GetRuleResults(int id)
        {
            var joins = "";
            var columns = "";

            var fields = Company.Filter<RuleResultQualifierType>(i => i.RuleID == id).OrderBy(i => i.Order).ToList();

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
        F.TextPath as FusionAttributePath,
        A.EffectiveDate,
        A.RunDate
from	RuleResult A  
        left join FusionAttribute F on F.ID = A.FusionAttributeID
        {joins} 
where   A.RuleID = {id} 
order by A.RunDate desc, A.EffectiveDate desc";

            var models = Company.Query<dynamic>(sql);

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, models);
        }
    }
}
