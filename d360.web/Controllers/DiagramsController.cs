using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using System.Collections.Generic;
using d360.core.enums;
using d360.web.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Design.PluralizationServices;
using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        #region Model Diagram

        [Route("{id:int}/InformationCatalogDiagramData")]
        public JsonNetResult InformationCatalogDiagramData(int id)
        {
            return new JsonNetResult {
                Data = Company.Query<InformationCatalogDiagramDataItem>(QueryConstants.InformationCatalogDiagramData, new { id = id }).ToList(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        List<InformationCatalogDiagramDataItem> loadInformationCatalogDiagramData(InformationCatalogDiagramDataItem model, List<InformationCatalogDiagramDataItem> rawItems)
        {
            if (rawItems.Any(i => (model != null) ? i.ParentID == model.ID : !i.ParentID.HasValue))
            {
                var list = new List<InformationCatalogDiagramDataItem>();
                foreach (var c in rawItems.Where(i => (model != null) ? i.ParentID == model.ID : !i.ParentID.HasValue).OrderBy(i => i.Name))
                {
                    c.Children = loadInformationCatalogDiagramData(c, rawItems);
                    list.Add(c);
                }
                return list;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Impact Analysis Diagram

        [Route("{type}/{id:int}/ImpactAnalysis")]
        public JsonNetResult ImpactAnalysis(SystemObjects type, int id)
        {

            var list = Company.Query<string>(QueryConstants.ImpactAnalysisDiagram, new {
                type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                id
            });
            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

            var json = string.Join("", list);
            //var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);
            dynamic obj = JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "{}" : json);

            if (obj != null && obj.nodes != null)
                foreach(var node in obj.nodes)
                {
                    try
                    {
                        node.typeNamePlural.Value = pluralize.IsPlural(node.typeNamePlural.Value) ? node.typeNamePlural.Value : pluralize.Pluralize(node.typeNamePlural.Value);
                    }
                    catch { }
                }

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }

        [Route("{type}/{id:int}/ImpactAnalysisFusion")]
        public JsonNetResult ImpactAnalysisFusion(SystemObjects type, int id)
        {
            var list = Company.Query<string>(QueryConstants.ImpactAnalysisDiagramFusion, new
            {
                type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                id
            });
            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

            var json = string.Join("", list);
            dynamic obj = JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "{}" : json);

            if (obj != null && obj.nodes != null)
                foreach (var node in obj.nodes)
                {
                    try
                    {
                        node.typeNamePlural.Value = pluralize.IsPlural(node.typeNamePlural.Value) ? node.typeNamePlural.Value : pluralize.Pluralize(node.typeNamePlural.Value);
                    }
                    catch { }
                }

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }
        #endregion

        #region Lineage Diagram

        [HttpGet, Route("{type}/{id:int}/lineage/{view:int}")]
        public JsonNetResult GetLineageByObject(SystemObjects type, int id, int view)
        {
            var list = Company.Query<string>(@"exec GetLineage @type, @id, @view", 
                new {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id,
                    view
                }
            ).ToList();

            var json = string.Join("", list);
            var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }

        #endregion
    }
}
