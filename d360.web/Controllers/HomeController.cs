using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using d360.web.Filters;
using System.Linq;
using d360.core.enums;
using d360.web.Models;
using System;

namespace d360.web.Controllers
{
    [HandleError(View = "Error")]
    public class HomeController : BaseController
    {
        #region DI

        public HomeController(CommunityContext community, CompanyContext company)
            : base(community, company) 
        { }

        #endregion

        /// <summary>
        /// Angular SPA
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult App()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("ResourceHomePage", Company.GetUserHomePage());
            ViewData.Add("Settings", Community.GetCompanySettings());
            ViewData.Add("SingleSignOn", IsSingleSignOn());

            var res = Company.GlobalReportingResources.Where(x => x.ResourceID == Company.CurrentResourceID).FirstOrDefault();
            if (res != null)
            {
                ViewData.Add("ResourceName", res.FullName);
                ViewData.Add("ResourceEmail", res.Email);
            }
            else
            {
                ViewData.Add("ResourceName", "");
                ViewData.Add("ResourceEmail", "");
            }
            return View("App");
        }

        [ValidateContracts(Ignore = true), Authorize, Route("terms")]
        public ViewResult Terms()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", Community.GetCompanySettings());
            
            var res = Company.GlobalReportingResources.Find(Company.CurrentResourceID);
            var orgs = Company.Filter<Organization>(i => i.AdministratorEmail == res.Email && (i.Accepted ?? false) == false && i.State == State.Active);
            var model = new TermsModel();
            var sql = @"select C.* from [Contract] C
                inner join dbo.GetContractValidations(@ResourceID) V on V.ContractID = C.ID
                where V.Accepted = 0";

            var contracts = Company.Query<Contract>(sql, new { ResourceID = Company.CurrentResourceID, res.Email }).ToList();
            model.Contracts = contracts.Select(c => new ContractModel(c)).ToList();

            return View(model);
        }

        [Authorize, Route("terms"), HttpPost]
        public ActionResult Terms(TermsModel model)
        {

            var resource = Company.GlobalReportingResources.FirstOrDefault(r => r.ResourceID == Company.CurrentResourceID);
            var invites = Company.OrganizationInvitations.Where(i => i.Email == resource.Email).ToList();
            var orgResources = Company.OrganizationResources.Where(o => o.ResourceID == Company.CurrentResourceID).ToList();

            model.Contracts.ForEach(c =>
            {
                if (c.Contract.OrganizationID.HasValue)
                {

                    var invite = invites.FirstOrDefault(i => i.OrganizationID == c.Contract.OrganizationID);
                    var orgRes = orgResources.FirstOrDefault(o => o.OrganizationID == c.Contract.OrganizationID);



                    if (orgRes == null && c.Contract.OrganizationID != null)
                    {
                        //add org resource record
                        orgRes = new OrganizationResource
                        {
                            ResourceID = Company.CurrentResourceID,
                            OrganizationID = (int)c.Contract.OrganizationID,
                            Accepted = true,
                            DateAccepted = DateTime.Now,

                        };

                        Company.Add(orgRes);
                    }
                    else
                    {
                        orgRes.Accepted = true;
                        orgRes.DateAccepted = DateTime.Now;
                        Company.Update(orgRes);
                    }

                    if (invite != null)
                        Company.Delete(invite); //remove the invite
                }


                c.Acceptance.ContractID = c.Contract.ID;
                c.Acceptance.OrganizationID = c.Contract.OrganizationID;
                c.Acceptance.Accepted = true;
                c.Acceptance.AcceptedOn = DateTime.Now;
                c.Acceptance.ResourceID = Company.CurrentResourceID;

                Company.Add(c.Acceptance);
            });

            return RedirectToAction("App");
        }

    }
}
