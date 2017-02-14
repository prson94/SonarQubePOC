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
using Dapper;

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

        [HttpPost, Route("{type}/{id:int}/lineagepreview/{view:int}")]
        public JsonNetResult GetLineagePreview(SystemObjects type, int id, int view, LineageEditorModel model)
        {

            var parameters = new DynamicParameters();
            var dt = new System.Data.DataTable();

            parameters.Add("type", type.ToString());
            parameters.Add("id", id);
            parameters.Add("view", view);

            #region DT Columns

            dt.Columns.Add(new System.Data.DataColumn("ID"));
            dt.Columns.Add(new System.Data.DataColumn("SourceIntersectID"));
            dt.Columns.Add(new System.Data.DataColumn("SourceSubject"));
            dt.Columns.Add(new System.Data.DataColumn("SourceSubjectID"));
            dt.Columns.Add(new System.Data.DataColumn("SourceObject"));
            dt.Columns.Add(new System.Data.DataColumn("SourceObjectID"));
            dt.Columns.Add(new System.Data.DataColumn("TargetIntersectID"));
            dt.Columns.Add(new System.Data.DataColumn("TargetSubject"));
            dt.Columns.Add(new System.Data.DataColumn("TargetSubjectID"));
            dt.Columns.Add(new System.Data.DataColumn("TargetObject"));
            dt.Columns.Add(new System.Data.DataColumn("TargetObjectID"));
            dt.Columns.Add(new System.Data.DataColumn("Deleting"));
            dt.Columns.Add(new System.Data.DataColumn("Adding"));

            #endregion

            if (model.Adds != null)
                foreach (var row in model.Adds)
                    dt.Rows.Add(row.ID,
                        row.SourceIntersectID,
                        row.SourceSubject,
                        row.SourceSubjectID,
                        row.SourceObject,
                        row.SourceObjectID,
                        row.TargetIntersectID,
                        row.TargetSubject,
                        row.TargetSubjectID,
                        row.TargetObject,
                        row.TargetObjectID,
                        false,
                        true);

            if (model.Deletes != null)
                foreach (var row in model.Deletes)
                    dt.Rows.Add(row.ID,
                        row.SourceIntersectID,
                        row.SourceSubject,
                        row.SourceSubjectID,
                        row.SourceObject,
                        row.SourceObjectID,
                        row.TargetIntersectID,
                        row.TargetSubject,
                        row.TargetSubjectID,
                        row.TargetObject,
                        row.TargetObjectID,
                        true,
                        false);

            dt.SetTypeName("LineageRow");
            parameters.Add("rows", dt);

            var list = Company.Query<string>("exec getlineage @type, @id, @view, @rows", parameters);

            var json = string.Join("", list);
            var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion
    }
}
