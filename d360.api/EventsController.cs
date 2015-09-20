using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.core;
using d360.services.interfaces;
using d360.core.entities;
using System.Xml.Linq;
using d360.extensions;
using System.Net.Http;
using System.Net;
using System.Runtime.Serialization;
using System.Web.Http.ModelBinding;
using Newtonsoft.Json.Linq;
using d360.core.exceptions;

namespace d360.api
{
    [RoutePrefix("events")]
    public class EventsController : BaseApiController
    {
        #region DI

        IEventService EventService;
        IFieldService FieldService;

        public EventsController(IEventService eventService, IFieldService fieldService, IAuthenticationSource authenticationSource)
        {
            EventService = eventService;
            FieldService = fieldService;
            AuthenticationSource = authenticationSource;
        }

        #endregion

        [Route("")]
        public IQueryable<EventType> GetEventTypes()
        {
            return EventService.GetEventTypes();
        }

        [Route("{typeID:int}/fields")]
        public IQueryable<FieldTypeWithRelation> GetEventTypeFieldTypes(int typeID)
        {
            return FieldService.GetFieldTypeRelationsByObject(SystemObjects.EventType, typeID);
        }

        [Route("{typeID}")]
        public CreateEventsModelResponse Post(int typeID, CreateEventsModelRequest model)
        {
            //var m = new CreateEventsModelRequest();
            //m.Events.Add(new CreateEventModelRequest());

            //var str = Newtonsoft.Json.JsonConvert.SerializeObject(m);
            //CreateEventsModelRequest model
            //CreateEventsModelRequest model = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateEventsModelRequest>(item.ToString());

            var response = new CreateEventsModelResponse();

            if (model == null)
            {
                var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
                msg.ReasonPhrase = "Request body is invalid.  Please reformat your request.";
                throw new HttpResponseException(msg);
            }

            var eventType = EventService.GetEventType(typeID);

            EventGroup eventGroup = null;

            if (string.IsNullOrEmpty(model.GroupKey))
            {
                model.GroupKey = Guid.NewGuid().ToString();
            }
            else
            {
                eventGroup = EventService.GetEventGroup(typeID, model.GroupKey);
            }

            try
            {
                if (eventGroup == null)
                {
                    eventGroup = new EventGroup { PublicID = model.GroupKey, EventTypeID = typeID, Name = string.IsNullOrEmpty(model.Name) ? "Event for " + model.GroupKey : model.Name };
                    EventService.AddEventGroup(eventGroup);
                }

                var fieldTypes = FieldService.GetFieldTypeRelationsByObject(SystemObjects.EventType, typeID).ToList();

                var sourceIDs = model.Events.Where(i => i.ContainsKey("SourceID")).Select(i => i["SourceID"]).ToList();
                var events = EventService.GetEventsByGroupIDAndSourceIDs(eventGroup.ID, sourceIDs).ToList();

                foreach (var log in model.Events)
                {
                    var logResponse = new CreateEventModelResponse();
                    var errorDetailMessage = "";
                    try
                    {
                        Event evt = null;
                        var sourceID = "";
                        if (log.ContainsKey("SourceID"))
                        {
                            sourceID = log["SourceID"];
                            evt = events.SingleOrDefault(i => i.SourceID == sourceID);
                        }

                        if (evt == null)
                        {
                            evt = new Event { Status = EventStatus.Open.ToString(), EventTypeID = typeID, EventGroupID = eventGroup.ID, SourceID = sourceID };
                            EventService.AddEvent(evt);
                        }

                        var fields = new List<Field>();
                        fieldTypes.ForEach(f =>
                        {
                            if (log.ContainsKey(f.Name))
                            {
                                var fld = new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Event.ToString(), ObjectID = evt.ID };
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

                        FieldService.AddOrUpdate(fields);

                        logResponse.ID = evt.ID;
                        logResponse.ResponseCode = HttpStatusCode.Created.ToString();
                        logResponse.ResponseMessage = "Created";
                    }
                    catch (Exception ex)
                    {
                        logResponse.ID = -1;
                        logResponse.ResponseCode = "500";
                        logResponse.ResponseMessage = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                        logResponse.ResponseMessage += errorDetailMessage;
                    }

                    response.Add(logResponse);
                }

                //response.ID = eventGroup.ID;

                return response;
            }
            catch (Exception ex)
            {
                var resMessage = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                resMessage.ReasonPhrase = (ex.InnerException == null) ? ex.Message : ex.InnerException.Message;
                throw new HttpResponseException(resMessage);
            }
        }
    }

    #region Models

    public class CreateEventsModelRequest
    {
        public CreateEventsModelRequest()
        {
            Events = new List<CreateEventModelRequest>();
        }

        public DateTime DateCreated { get; set; }
        //public int EventTypeID { get; set; }
        public string GroupKey { get; set; }
        public string Name { get; set; }

        public List<CreateEventModelRequest> Events { get; set; }
    }

    public class CreateEventModelRequest : Dictionary<string, string> {}

    public class CreateEventsModelResponse : List<CreateEventModelResponse>
    {
        //public CreateEventsModelResponse()
       // {
        //    Events = new List<CreateEventModelResponse>();
        //}

        //public int ID { get; set; }
        //public string GroupKey { get; set; }
        //public List<CreateEventModelResponse> Events { get; set; }
    }

    public class CreateEventModelResponse
    {
        public int ID { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
    }

    #endregion
}
