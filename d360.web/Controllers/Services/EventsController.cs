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

            if (Company.GetById<Policy>(model.PolicyID) == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("The Policy does not exist for ID: {0}.", model.PolicyID));

            try
            {
                var rule = new Rule
                {
                    Description = model.Description,
                    Name = model.Name,
                    PolicyID = model.PolicyID,
                    RuleType = model.RuleType
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
        /// <param name="id">The ID of the rule to add events to.</param>
        /// <param name="model">An object containing a collection of events, all associated to a group or job run in a source system.</param>
        /// <returns></returns>
        [Route("rules/{id}/events"), HttpPost]
        public HttpResponseMessage AddEvents(int id, CreateEventsModelRequest model)
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
                                errorDetailMessage += string.Format("{0}: Event does not contain {1} field {2}.  ", f.IsRequired ? "ERROR" : "WARNING",  f.IsRequired ? "required" : "optional", f.Name);
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
                        var status = EventStatus.Open;

                        if (log.ContainsKey("Criticality")) Enum.TryParse(log["Criticality"], out criticality);
                        if (log.ContainsKey("DateCreated")) DateTime.TryParse(log["DateCreated"], out dateCreated);
                        if (log.ContainsKey("Status")) Enum.TryParse(log["Status"], out status);

                        if (evt == null)
                        {
                            responseCode = HttpStatusCode.Created.ToString();

                            evt = new Event { Criticality = criticality, Status = status.ToString(), Date = dateCreated, EventGroupID = eventGroup.ID, SourceID = sourceID };
                            Company.SaveOrUpdate<Event>(evt);
                        }
                        else {
                            responseCode = HttpStatusCode.OK.ToString();

                            evt.Criticality = criticality;
                            evt.Status = status.ToString();
                            Company.SaveOrUpdate<Event>(evt);
                        }

                        fields.ForEach(f => {
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
                        logResponse.ResponseMessage = (ex.InnerException == null) ? ex.Message.Replace(Environment.NewLine, "") : ex.InnerException.Message.Replace(Environment.NewLine, "");
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
        /// 
        /// </summary>
        /// <param name="id">The ID of the rule to retrieve attributes for.</param>
        /// <param name="typeID">The ID of the attribute type to get.</param>
        /// <returns></returns>
        [Route("rules/{id}/attributes/{typeID}"), HttpGet]
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

    }
}
