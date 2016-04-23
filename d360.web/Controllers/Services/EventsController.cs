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

        public class CreateEventsModelRequest
        {
            public CreateEventsModelRequest()
            {
                Events = new List<CreateEventModelRequest>();
            }

            public string GroupKey { get; set; }
            public int? EventCount { get; set; }
            public string Name { get; set; }

            public List<CreateEventModelRequest> Events { get; set; }
        }

        public class CreateEventModelRequest : Dictionary<string, string>
        {
            //public EventCriticality? Criticality { get; set; }
            //public DateTime? DateCreated { get; set; }
            //public string SourceID { get; set; }
            //public EventStatus? Status { get; set; }
        }

        public class CreateEventModelResponse
        {
            public int ID { get; set; }
            public string SourceID { get; set; }
            public string ResponseCode { get; set; }
            public string ResponseMessage { get; set; }
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
                    rule = Company.Filter<Rule>(i => i.SourceID == model.SourceID).FirstOrDefault();
                    if (rule != null)
                    {
                        throw new ConflictException("Rule already exists", $"A rule with the source ID of {model.SourceID} already exists.");
                    }
                }

                rule = new Rule
                {
                    Description = model.Description,
                    Name = model.Name,
                    RuleType = model.RuleType,
                    SourceID = model.SourceID,
                    RuleDimensionID = model.RuleDimensionID
                };

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
        /// <param name="model">An object containing a collection of events, all associated to a group or job run in a source system.</param>
        /// <returns></returns>
        [Route("sourcerules/{sourceID}/events"), HttpPost]
        public HttpResponseMessage AddSourceRuleEvents(string sourceID, CreateEventsModelRequest model)
        {
            var rule = Company.Filter<Rule>(i => i.SourceID == sourceID).FirstOrDefault();

            if (rule != null)
            {
                return AddRuleEvents(rule.ID, model);
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
        /// <param name="model">An object containing a collection of events, all associated to a group or job run in a source system.</param>
        /// <returns></returns>
        [Route("rules/{id:int}/events"), HttpPost]
        public HttpResponseMessage AddRuleEvents(int id, CreateEventsModelRequest model)
        {
            if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update, ClaimObject.Root))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add events to this rule.");

            var responseModels = new List<CreateEventModelResponse>();

            if (model == null)
            {
                var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
                msg.ReasonPhrase = "Request body is invalid.  Please reformat your request.";
                throw new HttpResponseException(msg);
            }

            var rule = Company.GetById<Rule>(id);

            EventGroup eventGroup = null;

            if (string.IsNullOrEmpty(model.GroupKey))
            {
                model.GroupKey = Guid.NewGuid().ToString();
            }
            else
            {
                eventGroup = Company.Filter<EventGroup>(i => i.RuleID == id && i.PublicID == model.GroupKey).FirstOrDefault();
            }

            try
            {
                var sType = SystemObjects.Rule.ToString();
                var fieldTypes = Company.Filter<FieldType>(i => i.Object == sType && i.ObjectID == id).ToList();

                if (eventGroup == null)
                {
                    eventGroup = new EventGroup { PublicID = model.GroupKey, RuleID = id, Name = string.IsNullOrEmpty(model.Name) ? "Event for " + model.GroupKey : model.Name };

                }

                if (rule.RuleType == RuleType.Metric || rule.RuleType == RuleType.Profile)
                {
                    if (model.EventCount.HasValue)
                        eventGroup.EventCount = model.EventCount.Value;
                }
                Company.SaveOrUpdate<EventGroup>(eventGroup);

                var sourceIDs = model.Events.Where(i => i.ContainsKey("SourceID")).Select(i => i["SourceID"]).ToList();
                var events = Company.Filter<Event>(i => i.EventGroupID == eventGroup.ID && sourceIDs.Contains(i.SourceID));

                foreach (var log in model.Events)
                {
                    var logResponse = new CreateEventModelResponse();
                    var errorDetailMessage = "";
                    try
                    {
                        #region Add fields that do not yet exists in D3S

                        try
                        {
                            foreach (var key in log.Keys)
                            {
                                if (key != "Criticality"
                                    && key != "DateCreated"
                                    && key != "SourceID"
                                    && key != "Status"
                                    && !fieldTypes.Any(i => i.Name == key))
                                {
                                    var newFieldType = new FieldType { Object = sType, ObjectID = id, IsRequired = false, IsListable = true, SortOrder = fieldTypes.Count + 1, FriendlyName = key, Name = key, DisplayDescription = "", FormDescription = "", Type = "Text" };
                                    Company.Add<FieldType>(newFieldType);
                                    fieldTypes.Add(newFieldType);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }

                        #endregion


                        var fields = new List<Field>();
                        fieldTypes.ForEach(f =>
                        {
                            if (log.ContainsKey(f.Name))
                            {
                                var fld = new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Event.ToString() };
                                if (log[f.Name] == null)
                                {
                                    if (f.IsRequired)
                                    {
                                        errorDetailMessage += string.Format("ERROR: Event does not contain required field {0}.  ", f.Name);
                                        throw new MissingPropertiesException("Event");
                                    }
                                }
                                else
                                {
                                    fld.Value = log[f.Name];
                                    fields.Add(fld);
                                }
                            }
                            else
                            {
                                errorDetailMessage += string.Format("{0}: Event does not contain {1} field {2}.  ", f.IsRequired ? "ERROR" : "WARNING", f.IsRequired ? "required" : "optional", f.Name);
                                if (f.IsRequired)
                                {
                                    throw new MissingPropertiesException("Event");
                                }
                            }
                        });

                        #region If you made it this far in loop, fields for this event are valid.

                        Event evt = null;
                        var sourceID = "";
                        if (log.ContainsKey("SourceID"))
                        {
                            sourceID = log["SourceID"];
                            evt = events.SingleOrDefault(i => i.SourceID == sourceID);
                        }

                        var responseCode = "";

                        var dateCreated = DateTime.UtcNow;
                        var criticality = EventCriticality.Negligible;
                        var status = "Open";

                        if (log.ContainsKey("Criticality")) Enum.TryParse(log["Criticality"], out criticality);
                        if (log.ContainsKey("DateCreated")) DateTime.TryParse(log["DateCreated"], out dateCreated);
                        if (log.ContainsKey("Status")) status = log["Status"];

                        if (evt == null)
                        {
                            responseCode = HttpStatusCode.Created.ToString();

                            evt = new Event { Criticality = criticality, Status = status.ToString(), Date = dateCreated, EventGroupID = eventGroup.ID, SourceID = sourceID };
                            Company.SaveOrUpdate<Event>(evt);
                        }
                        else
                        {
                            responseCode = HttpStatusCode.OK.ToString();

                            evt.Criticality = criticality;
                            evt.Status = status.ToString();
                            Company.SaveOrUpdate<Event>(evt);
                        }

                        fields.ForEach(f =>
                        {
                            f.ObjectID = evt.ID;
                        });

                        #endregion

                        Company.AddOrUpdateFields(fields);

                        logResponse.ID = evt.ID;
                        logResponse.SourceID = evt.SourceID;
                        logResponse.ResponseCode = responseCode;
                        logResponse.ResponseMessage = errorDetailMessage;
                    }
                    catch (Exception ex)
                    {
                        logResponse.ID = -1;
                        logResponse.ResponseCode = "500";
                        logResponse.ResponseMessage = (ex.InnerException == null) ? ex.Message.Replace(System.Environment.NewLine, "") : ex.InnerException.Message.Replace(System.Environment.NewLine, "");
                        logResponse.ResponseMessage += errorDetailMessage;
                    }

                    responseModels.Add(logResponse);
                }

                //response.ID = eventGroup.ID;

                return Request.CreateResponse<List<CreateEventModelResponse>>(HttpStatusCode.OK, responseModels);
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
        /// Add one or more relationships to a rule based on the source ID from an underlying system that originally created the rule.
        /// </summary>
        /// <param name="sourceID">The underlying source ID of the system the the rule originated from.</param>
        /// <param name="models">A collection of relationships.</param>
        /// <returns></returns>
        [Route("sourcerules/{sourceID}/relationships"), HttpPost]
        public HttpResponseMessage AddSourceRuleRelationships(string sourceID, List<ObjectModel> models)
        {
            var rule = Company.Filter<Rule>(i => i.SourceID == sourceID).FirstOrDefault();

            if (rule != null)
            {
                return addRuleRelationships(rule.ID, models, rule);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = $"Rule could not be located based on the Source ID: {sourceID}." });
            }
        }

        /// <summary>
        /// Add one or more relationships to a rule.
        /// </summary>
        /// <param name="id">The ID of the rule to add events to.</param>
        /// <param name="models">A collection of relationships.</param>
        /// <returns></returns>
        [Route("rules/{id:int}/relationships"), HttpPost]
        public HttpResponseMessage AddRuleRelationships(int id, List<ObjectModel> models)
        {
            return addRuleRelationships(id, models);
        }

        HttpResponseMessage addRuleRelationships(int id, List<ObjectModel> models, Rule rule = null)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update, ClaimObject.Root))
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { Message = "You are not allowed to add relationships to this rule." });

                if (models == null)
                {
                    throw new MissingPropertiesException("Rule Relationships");
                }
                else
                {
                    if (models.Count == 0)
                    {
                        throw new MissingPropertiesException("Rule Relationships");
                    }
                }

                if (rule == null) //If no rule sent in, do a lookup.
                {
                    rule = Company.GetById<Rule>(id);
                }

                if (rule == null)
                {
                    throw new NotFoundException("Rule");
                }
                models.ForEach(m =>
                {
                    var t = (SystemObjects)Enum.Parse(typeof(SystemObjects), m.ObjectType);
                    Company.AddRelationship(SystemObjects.Rule, id, t, m.ObjectID, IntersectClassification.Normal, null, null);
                });
                //Company.AddRelationships(SystemObjects.Rule, id, IntersectClassification.Normal, null, null, models);

                return Request.CreateResponse(HttpStatusCode.Created, new { Message = "Relationships created." });
            }
            catch (BaseException ex)
            {
                return Request.CreateResponse(ex.StatusCode, new { Message = ex.StatusMessage });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = $"An unknown error occured.  Please try again later. Error was: {ex.GetFullExceptionData()}" });
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">The ID of the rule to retrieve attributes for.</param>
        /// <param name="typeID">The ID of the attribute type to get.</param>
        /// <returns></returns>
        [Route("rules/{id:int}/attributes/{typeID:int}"), HttpGet]
        public HttpResponseMessage GetAttributesByAttributeType(int id, int typeID)
        {
            HttpResponseMessage response = null;

            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(typeID, "Attribute", out joins, out columns);

            var querySql = string.Format(@"select A.ID, {0} T.Name
from	Attribute A 
inner join AttributeType T on T.ID = A.AttributeTypeID and T.ID = @typeID and A.ObjectType = 'Rule' and A.ObjectID = @id {1}", columns, joins);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            var models = Company.Query<dynamic>(sql, new { id = id, typeID = typeID });
            response = Request.CreateResponse(HttpStatusCode.OK, models);

            return response;
        }


        /// <summary>
        /// GEts all events for a rule.
        /// </summary>
        /// <param name="id">The ID of the rule to get events from.</param>
        /// <returns></returns>
        [Route("rules/{id:int}/events"), HttpGet]
        public HttpResponseMessage GetRuleEvents(int id)
        {
            var joins = "";
            var columns = "";

            var fields = Company.Filter<FieldTypeWithRelation>(i => i.Object == "Rule" && i.ObjectID == id).OrderBy(i => i.SortOrder).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");
                columns += $"{name}_T.FormattedValue as [{name}], ";
                joins += $" left join FieldWithRelation {name}_T on {name}_T.ObjectType = 'Event' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {f.ID}";
            }


            var sql = $@"
select	A.ID,
		A.SourceID,
		A.Status,
        A.Criticality,
		V.Name as EventGroup,
        V.PublicID as PublicID,
        {columns}
		A.[Date]
from	[Event] A inner join EventGroup V on V.ID = A.EventGroupID and V.RuleID = {id} 
        {joins}";

            var models = Company.Query<dynamic>(sql);

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, models);
        }
    }
}
