using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using SpreadsheetLight;
using System.IO;

namespace d360.web.Controllers
{
    [RoutePrefix("domains"), Authorize]
    public class DomainsController : BaseController
    {
        #region DI

        public DomainsController(CommunityContext community, CompanyContext company): base(community, company)
        {
        }

        #endregion

        [Route("Hierarchy")]
        public JsonNetResult Hierarchy(int id)
        {
            var dt = Company.GetById<DomainType>(id, i => i.Domains, i => i.Groups);

            var list = new List<DomainHierarchyItem>();

            foreach (var o in dt.Groups.OrderBy(o => o.Name))
            {
                list.Add(new DomainHierarchyItem 
                {
                    HierarchyID = string.Format("DomainGroup|{0}", o.ID),
                    ID = o.ID,
                    Name = o.Name,
                    ParentHierarchyID = null,
                    Type = "DomainGroup"
                });
            }

            foreach (var o in dt.Domains.OrderBy(o => o.Name))
            {
                list.Add(new DomainHierarchyItem
                {
                    HierarchyID = string.Format("Domain|{0}", o.ID),
                    ID = o.ID,
                    Name = o.Name,
                    ParentHierarchyID = string.Format("DomainGroup|{0}", o.DomainGroupID),
                    Type = "Domain"
                });
            }

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #region Exports

        //[Route("{id:int}.xlsx"), FileDownload, HttpGet]
        //public FileResult ToExcel(int id)
        //{
        //    var domain = Company.GetById<Domain>(id, i => i.Items);
        //    //var items = Company.Filter<DomainItem>(i => i.DomainID == id);

        //    var document = new SLDocument();
        //    document.AddWorksheet("Items");

        //    #region Create the list sheet

        //    int r = 1;

        //    #region Header

        //    document.SetCellValue(r, 1, "Name");
        //    document.SetCellValue(r, 2, "Code");
        //    document.SetCellValue(r, 3, "Description");

        //    #endregion

            
        //    foreach(var item in domain.Items.OrderBy(i => i.Name))
        //    {
        //        r++;
        //        document.SetCellValue(r, 1, item.Name);
        //        document.SetCellValue(r, 2, item.Code);
        //        document.SetCellValue(r, 3, item.Description);
        //    }

        //    #endregion

        //    var stream = new MemoryStream();
        //    document.SaveAs(stream);
        //    return File(stream.ToArray(), "application/vnd.ms-excel", $"{domain.Name} - Items.xlsx");
        //}

        #endregion
    }
}
