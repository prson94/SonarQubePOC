using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.services.interfaces;
using d360.extensions;
using d360.core.entities;
using System.Net.Http;
using System.Net;
using d360.core.exceptions;
using System.Xml.Linq;
using d360.core;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Xml;
using System.Text.RegularExpressions;

namespace d360.api
{
    [RoutePrefix("fusion")]
    public class FusionController : BaseApiController
    {
        #region DI

        IFieldService FieldService;
        IFusionService FusionService;
        IResourceService ResourceService;

        public FusionController(IFieldService fieldService, IFusionService fusionService, IResourceService resourceService, IAuthenticationSource authenticationSource)
        {
            FieldService = fieldService;
            FusionService = fusionService;
            ResourceService = resourceService;
            AuthenticationSource = authenticationSource;
        }

        #endregion

        [Route("configurations")]
        public HttpResponseMessage GetAllConfigurations()
        {
            var items = FusionService.GetFusionsAsDictionary(true);
            return Request.CreateResponse<List<Dictionary<string, object>>>(HttpStatusCode.OK, items);
        }

        [Route("{typeID:int}/configurations")]
        public HttpResponseMessage GetConfigurationsByType(int typeID)
        {
            var type = FusionService.GetType(typeID);
            if (type == null) return Request.CreateResponse(HttpStatusCode.NotFound);

            var items = FusionService.GetFusionsAsDictionary(typeID, true);
            return Request.CreateResponse<List<Dictionary<string, object>>>(HttpStatusCode.OK, items);
        }

        [HttpPost, Route("{typeID:int}/configurations/{fusionID:int}/attributes/{fusionAttributeTypeID:int}")]
        public HttpResponseMessage PostAttributes(int typeID, int fusionID, int fusionAttributeTypeID, List<Dictionary<string, string>> models)
        {
            var responses = new List<APIResponse>();
            FusionAttribute item = null;

            try
            {
                var type = FusionService.GetAttributeType(fusionAttributeTypeID);

                // Check that type was found
                if (type == null) throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

                var fieldTypes = FieldService.GetFieldTypesByObject(SystemObjects.FusionAttributeType, fusionAttributeTypeID).ToList();

                models.ForEach(model =>
                //models.AsParallel().ForAll(model =>
                {
                    try
                    {
                        var name = model["Name"];
                        var sourceID = model["SourceID"];

                        int? parentID = null;

                        if (model.ContainsKey("ParentID"))
                        {
                            if (!string.IsNullOrEmpty(model["ParentID"]))
                            {
                                parentID = int.Parse(model["ParentID"]);
                            }
                        }

                        item = FusionService.GetAttribute(fusionID, fusionAttributeTypeID, sourceID, FusionAttributeIdentifier.BySourceID, parentID);

                        HttpStatusCode status;
                        if (item == null)
                        {
                            item = new FusionAttribute { FusionAttributeTypeID = fusionAttributeTypeID, FusionID = fusionID, Name = name, ParentID = parentID, SourceID = sourceID };
                            FusionService.AddAttribute(item);

                            status = HttpStatusCode.Created;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(item.SourceID))
                            {
                                item.SourceID = sourceID;
                                FusionService.EditAttribute(item);
                            }

                            status = HttpStatusCode.OK;
                        }

                        #region Add or Update dynamic fields regardless if insert or update.

                        var fields = new List<Field>();
                        fieldTypes.ForEach(f =>
                        {
                            if (model.ContainsKey(f.Name))
                            {
                                try
                                {
                                    fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.FusionAttribute.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString() });
                                }
                                catch
                                {
                                }
                            }
                        });
                        FieldService.AddOrUpdate(fields);

                        #endregion

                        var response = new APIResponse { ID = item.ID, SourceID = item.SourceID, Name = item.Name, ResponseCode = status.ToString(), ResponseMessage = "SUCCESS" };
                        //lock (responses)
                        responses.Add(response);
                    }
                    catch (Exception ex)
                    {
                        var response = new APIResponse { ResponseCode = HttpStatusCode.NotAcceptable.ToString(), ResponseMessage = ex.Message };
                        //lock (responses)
                        responses.Add(response);
                    }
                });

                return Request.CreateResponse<List<APIResponse>>(HttpStatusCode.OK, responses);
            }
            catch (BaseException ex)
            {
                var msg = new HttpResponseMessage(ex.StatusCode);
                msg.ReasonPhrase = ex.StatusDescription;
                throw new HttpResponseException(msg);
            }
            catch (Exception ex)
            {
                var msg = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                msg.ReasonPhrase = ex.InnerException != null ? ex.InnerException.Message.Replace(@"\n", "; ") : ex.Message.Replace(@"\n", "; ");
                throw new HttpResponseException(msg);
            }
        }

        private static Regex _invalidXMLChars = new Regex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled);

        [HttpPost, Route("{typeID:int}/configurations/{fusionID:int}/attributes")]
        public HttpResponseMessage PostBulkAttributes(int typeID, int fusionID, BulkFusionImport import)//List<Dictionary<string, string>> models)
        {
            try
            {
                #region Serialize Raw Data
                
                try
                {
                    string rawData = JsonConvert.SerializeObject(import);
                    ResourceService.AddRawApiMessage(SystemObjects.Fusion, fusionID, rawData);
                }
                catch (Exception ex)
                {
                    ResourceService.AddApiError(SystemObjects.Fusion, fusionID, ex);
                }

                #endregion

                var mXml = new XElement("ms");
                var rowNumber = 1;
                import.Models.ForEach(o => {
                    var m = new XElement("m", new XAttribute("id", rowNumber));
                    
                    try
                    {
                        foreach (var k in o.Keys)
                        {
                            var value = (o[k] == null) ? "" : o[k];
                            var key = _invalidXMLChars.Replace(k, "");

                            if (value.Contains("<") || value.Contains(">"))
                                m.Add(new XElement(key, new XCData(value)));
                            else
                            {
                                m.Add(new XElement(key, _invalidXMLChars.Replace(value, "")));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ResourceService.AddApiError(SystemObjects.Fusion, fusionID, ex);
                    }

                    mXml.Add(m);
                    rowNumber++;
                });

                var rXml = new XElement("rs");

                try
                {
                    import.Relationships.ForEach(o =>
                    {
                        rXml.Add(new XElement("r", new XAttribute("s", o.StartID), new XAttribute("e", o.EndID)));
                    });
                }
                catch (Exception ex)
                {
                    ResourceService.AddApiError(SystemObjects.Fusion, fusionID, ex);
                }

                var doc = new XElement("import", mXml, rXml);

                //var xs = new DataContractSerializer(import.GetType());
                //MemoryStream stm = new MemoryStream();
                //xs.WriteObject(stm, import);
                //stm.Seek(0, SeekOrigin.Begin);
                //using (var streamReader = new StreamReader(stm))
                //{
                    var queueItem = new QueueItem
                    {
                        ObjectType = "Fusion",
                        ObjectID = fusionID,
                        Action = "ProcessFusionForObjectAction",
                        Date = DateTime.UtcNow,
                        Data = doc.ToString()//streamReader.ReadToEnd()//JsonConvert.SerializeObject(import)
                    };
                    ResourceService.AddQueueItem(queueItem);
                //}
                return Request.CreateResponse<string>(HttpStatusCode.OK, "Now parsing items");
            }
            catch (BaseException ex)
            {
                ResourceService.AddApiError(SystemObjects.Fusion, fusionID, ex);

                var msg = new HttpResponseMessage(ex.StatusCode);
                msg.ReasonPhrase = ex.StatusDescription;
                throw new HttpResponseException(msg);
            }
            catch (Exception ex)
            {
                ResourceService.AddApiError(SystemObjects.Fusion, fusionID, ex);

                var msg = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                msg.ReasonPhrase = ex.InnerException != null ? ex.InnerException.Message.Replace(@"\n", "; ") : ex.Message.Replace(@"\n", "; ");
                throw new HttpResponseException(msg);
            }
        }

        [HttpPost, Route("{typeID:int}/configurations/{fusionID:int}/job")]
        public HttpResponseMessage PostStartSynchronizationInstance(int typeID, int fusionID)
        {
            var message = new HttpResponseMessage();

            var success = FusionService.StartFusionInstance(fusionID);

            if (success)
            {
                message.StatusCode = HttpStatusCode.Created;
            }
            else
            {
                message.StatusCode = HttpStatusCode.BadRequest;
                message.ReasonPhrase = "Error occured when trying to start a new instance for this configuration.";
            }

            return message;        
        }

        [HttpPut, Route("{typeID:int}/configurations/{fusionID:int}/job")]
        public HttpResponseMessage PutEndSynchronizationInstance(int typeID, int fusionID)
        {
            var message = new HttpResponseMessage();

            var success = FusionService.CompleteFusionInstance(fusionID);

            if (success)
            {
                message.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                message.StatusCode = HttpStatusCode.BadRequest;
                message.ReasonPhrase = "Error occured when trying to complete the latest instance for this configuration.";
            }

            return message;
        }

        [HttpPost, Route("test")]
        public HttpResponseMessage PostTest(TestPackage package)
        {
            var message = new HttpResponseMessage();

            var success = !string.IsNullOrEmpty(package.Message);

            if (success)
            {
                message.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                message.StatusCode = HttpStatusCode.BadRequest;
                message.ReasonPhrase = "Error occured when detecting a valid message in this test.  Message is NULL or empty.";
            }

            return message;
        }
    }
}
