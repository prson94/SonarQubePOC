using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using d360.model;
using d360.web.Models;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(CoreComponentSet set) : base(set) { }

        #endregion

        #region Model Diagram

        [Route("{id:int}/InformationCatalogDiagramData")]
        public JsonNetResult InformationCatalogDiagramData(int id)
        {
            return new JsonNetResult
            {
                Data = Company.Query<InformationCatalogDiagramDataItem>(QueryConstants.InformationCatalogDiagramData, new { id }).ToList(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        private List<InformationCatalogDiagramDataItem> loadInformationCatalogDiagramData(InformationCatalogDiagramDataItem model, List<InformationCatalogDiagramDataItem> rawItems)
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
    }
}
