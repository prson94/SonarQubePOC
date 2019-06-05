using d360.core;
using d360.core.helpers;
using d360.model;
using d360.web.Models;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Linq;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(ICommunityContext community, ICompanyContext company) : base(community, company) { }

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

            var json = string.Join("", list);

            dynamic obj = JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "{}" : json);

            if (obj != null && obj.nodes != null && PluralCultureHelper.IsNeutralCultureEnglish())
            {
                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

                foreach (var node in obj.nodes)
                {
                    try
                    {
                        node.typeNamePlural.Value = pluralize.IsPlural(node.typeNamePlural.Value) ?
                            node.typeNamePlural.Value :
                            pluralize.Pluralize(node.typeNamePlural.Value);
                    }
                    catch { }
                }
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

            var json = string.Join("", list);
            dynamic obj = JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "{}" : json);

            if (obj != null && obj.nodes != null && PluralCultureHelper.IsNeutralCultureEnglish())
            {
                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

                foreach (var node in obj.nodes)
                {
                    try
                    {
                        node.typeNamePlural.Value = pluralize.IsPlural(node.typeNamePlural.Value) ? node.typeNamePlural.Value : pluralize.Pluralize(node.typeNamePlural.Value);
                    }
                    catch { }
                }
            }

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }
        #endregion

        #region Lineage Diagram

        [HttpGet, Route("{type}/{id:int}/lineage/{view:int}/{usageOnly:bool}")]
        public JsonNetResult GetLineageByObject(SystemObjects type, int id, int view, bool usageOnly)
        {
            var verboseLineage = Community.GetCompanySettings().Single(s => s.Key == "EnableVersion1VerboseLineage");

            var list = Company.Query<string>(@"exec GetLineage @type, @id, @view, @usageOnly, @verboseLineage", 
                new {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id,
                    view,
                    usageOnly,
                    verboseLineage = (verboseLineage.Value == "true")
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
        public JsonNetResult GetLineagePreview(SystemObjects type, int id, int view, LineagePreviewModel model)
        {

            LineageEditorModel businessModel = model.BusinessModel;
            LineageEditorTechnicalModel technicalModel = model.TechnicalModel;

            var parameters = new DynamicParameters();
            var dtBusiness = new System.Data.DataTable();
            var dtTechnical = new System.Data.DataTable();

            parameters.Add("type", type.ToString());
            parameters.Add("id", id);
            parameters.Add("usageOnly", false);
            parameters.Add("view", view);

            #region DT Columns

            dtBusiness.Columns.Add(new System.Data.DataColumn("ID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceIntersectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceSubject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceSubjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceObject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceObjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetIntersectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetSubject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetSubjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetObject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetObjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("Deleting"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("Adding"));

            dtTechnical.Columns.Add(new System.Data.DataColumn("ID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("MapItemID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("SourceFusionAttributeID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("TargetFusionAttributeID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("Deleting"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("Adding"));

            #endregion
            if (businessModel != null)
            {
                if (businessModel.Adds != null)
                    foreach (var row in businessModel.Adds)
                        dtBusiness.Rows.Add(row.ID,
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

                if (businessModel.Deletes != null)
                    foreach (var row in businessModel.Deletes)
                        dtBusiness.Rows.Add(row.ID,
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
            }

            if (technicalModel != null)
            {
                if (technicalModel.Adds != null)
                    foreach (var row in technicalModel.Adds)
                        dtTechnical.Rows.Add(row.ID, row.MapItemID, row.SourceFusionAttributeID, row.TargetFusionAttributeID, false, true);

                if (technicalModel.Deletes != null)
                    foreach (var row in technicalModel.Deletes)
                        dtTechnical.Rows.Add(row.ID, row.MapItemID, row.SourceFusionAttributeID, row.TargetFusionAttributeID, true, false);
            }


            dtBusiness.SetTypeName("LineageTable");
            parameters.Add("rows", dtBusiness);

            dtTechnical.SetTypeName("LineageTechnicalTable");
            parameters.Add("technicalRows", dtTechnical);

            var verboseLineage = Community.GetCompanySettings().Single(s => s.Key == "EnableVersion1VerboseLineage");

            parameters.Add("verboseLineage", (verboseLineage.Value == "true"));

            var list = Company.Query<string>("exec GetLineage @type, @id, @view, @usageOnly, @verboseLineage, @rows, @technicalRows", parameters);

            var json = string.Join("", list);
            var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("{type}/{id:int}/lineagenode")]
        public JsonNetResult GetLineageNodeDataForObject(string type, int id)
        {
            var sql = @"select 
	DisplayValue as [name],
	ForeColor as foreColor,
	BackColor as backColor,
	[object],
	objectId,
	Type as objectType,
	TypeID as objectTypeId,
	TypeName as objectTypeName
from AssetDetail
where [object] = @type and objectid = @id";

            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(sql, new { type, id }).FirstOrDefault(),
                Formatting = Formatting.None
            };
        }

        #endregion
    }
}
