using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using d360.web.Filters;
using System.Linq;
using d360.core.enums;
using d360.web.Models;
using d360.web.Models.Attributes;
using System;
using d360.extensions.caching;

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
        [ValidateContracts, Authorize]
        public ActionResult App()
        {

            if (!updateContractValidationCache())
                return RedirectToAction("terms");

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
            
            var model = new TermsModel();
            var validations = Company.Query<ContractValidation>(@"select * from dbo.GetContractValidations(@ResourceID)", new { ResourceID = Company.CurrentResourceID });

            validations = validations.Where(v => !v.Accepted && ((v.IsFirstUser && v.ContractType == ContractType.OrganizationTermsOfUse) || v.ContractType == ContractType.ResourceTermsOfUse || v.OrganizationID == null));

            if (validations.Any())
            {
                model.Contracts = new System.Collections.Generic.List<ContractModel>();

                validations.ToList().ForEach(v =>
                {
                    var contract = Company.GetById<Contract>(v.ContractID);
                    if (contract != null)
                        model.Contracts.Add(new ContractModel(contract));

                });

                model.Contracts.OrderBy(c => c.Contract.ContractType).ThenBy(c => !c.Contract.OrganizationID.HasValue ? 0 : 1);
            }

            return View(model);
        }

        [ValidateContracts(Ignore = true), Authorize, Route("terms"), HttpPost]
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

                    if (c.Contract.ContractType == ContractType.OrganizationTermsOfUse)
                    {
                        var org = Company.GetById<Organization>((int)c.Contract.OrganizationID);
                        if (org != null)
                        {
                            org.Accepted = true;
                            org.AcceptedBy = Company.CurrentResourceID;
                            org.DateAccepted = DateTime.Now;
                            Company.Update(org);
                        }
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

            updateContractValidationCache();

            return RedirectToAction("App");
        }


        private bool updateContractValidationCache()
        {
            var key = ContractValidationCacheModel.cacheKey;
            var cache = new MemoryCachingProvider();
            var time = ContractValidationCacheModel.cacheDuration;
            var cacheRes = cache.GetItemInListByID<ContractValidationCacheModel.User, int>(key, Company.CurrentResourceID);
            var contractCount = Company.Query<int>(@"select count(*) from dbo.GetContractValidations(@ResourceID) where accepted = 0", new { ResourceID = Company.CurrentResourceID }).FirstOrDefault();
            var contractsAccepted = contractCount == 0;

            
            if (cacheRes != null)
            {
                var com = cacheRes.Companies.FirstOrDefault(c => c.ID == Company.CurrentCompanyID);
                if (com != null)
                {
                    if (!com.ContractsAccepted)
                    {
                        contractsAccepted = contractCount == 0;
                        com.ContractsAccepted = contractCount == 0;
                        cache.SetItemInListByID(key, Company.CurrentResourceID, cacheRes, true, time);
                    }
                }
                else
                {
                    cacheRes.Companies.Add(new ContractValidationCacheModel.Company() { ID = Company.CurrentCompanyID, ContractsAccepted = contractCount == 0 });
                    cache.SetItemInListByID(key, Company.CurrentResourceID, cacheRes, true, time);
                }
            }
            else
            {
                cacheRes = new ContractValidationCacheModel.User();
                cacheRes.Companies.Add(new ContractValidationCacheModel.Company() { ID = Company.CurrentCompanyID, ContractsAccepted = contractCount == 0 });
                cache.SetItemInListByID(key, Company.CurrentResourceID, cacheRes, true, time);

            }

            return contractsAccepted;
        }

    }
}
