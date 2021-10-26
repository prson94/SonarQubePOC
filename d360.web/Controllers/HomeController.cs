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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using d360.web.caching;
using d360.model.DataAccessLayer;
using d360.extensions;

namespace d360.web.Controllers
{
    [HandleError(View = "Error")]
    public class HomeController : BaseController
    {
        #region DI

        readonly ICachingProvider Cache;

        public HomeController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository, ICachingProvider cache)
            : base(community, company, settingsRepository)
        {
            Cache = cache;
        }

        #endregion


        [ValidateContracts(Ignore = true), AllowAnonymous, Route("unsupported")]
        public ActionResult Unsupported()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            return View("Unsupported");
        }

        /// <summary>
        /// Angular SPA
        /// </summary>
        /// <returns></returns>
        [ValidateContracts, Authorize]
        public async Task<ActionResult> App()
        {
            if (!updateContractValidationCache())
                return RedirectToAction("terms", new { redirectUri = HttpContext.Request.Path });

            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("ResourceHomePage", Company.GetUserHomePage());
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());
            ViewData.Add("EnvironmentSettings", new Dictionary<string, string> { { "HelpBaseUri", System.Configuration.ConfigurationManager.AppSettings["HelpBaseUri"].ToString() } });
            ViewData.Add("SingleSignOn", await IsSingleSignOn());

            var res = Company.GlobalReportingResources.Where(x => x.ResourceID == Company.CurrentResourceID).FirstOrDefault();
            if (res != null)
            {
                ViewData.Add("ResourceName", res.FullName);
                ViewData.Add("ResourceEmail", res.Email);
                ViewData.Add("ResourceUid", res.Uid);
            }
            else
            {
                ViewData.Add("ResourceName", "");
                ViewData.Add("ResourceEmail", "");
                ViewData.Add("ResourceUid", "");
            }


            return View("App");
        }

        [ValidateContracts(Ignore = true), Authorize, Route("terms")]
        public ActionResult Terms(string redirectUri = null)
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("Settings", SettingsRepository.GetSettingsAsDictionary());

            var validations = Company.Query<ContractValidation>(@"select * from dbo.GetContractValidations(@ResourceID)", new { ResourceID = Company.CurrentResourceID });

            validations = validations.Where(v => !v.Accepted && ((v.IsFirstUser && v.ContractType == ContractType.OrganizationTermsOfUse) || v.ContractType == ContractType.ResourceTermsOfUse || v.OrganizationID == null));

            if (validations.Any())
            {
                

                ContractValidation contractValidation = validations.OrderBy(v => (int)v.ContractType).ThenBy(v => v.OrganizationID.HasValue ? 1 : 0).First();
                var contract = Company.GetById<Contract>(contractValidation.ContractID);
                var orgs = validations.Where(v => v.ContractType == ContractType.OrganizationTermsOfUse && v.OrganizationID != null).Select(v => (int)v.OrganizationID).Distinct();


                var termsModel = new TermsModel(contract);
                termsModel.RedirectUri = redirectUri;
                termsModel.IsLastContract = validations.Count() == 1;

                if (contract.OrganizationID != null && contract.ContractType == ContractType.OrganizationTermsOfUse && validations.Count(v => v.ContractID == contract.ID) == 1)
                    termsModel.IsLastOrgContract = true;

                if (orgs.Count() > 0 && validations.Count(v => v.ContractType == ContractType.OrganizationTermsOfUse && v.OrganizationID == null) > 0)
                    termsModel.OrgsWithContracts = orgs.ToList();

                    return View(termsModel);

            }
            else
            {
                if (!string.IsNullOrEmpty(redirectUri) && !redirectUri.StartsWith("//") && Uri.IsWellFormedUriString(redirectUri, UriKind.Relative))
                    return Redirect(redirectUri);
                else
                    return RedirectToAction("App");
            }
        }

        [ValidateContracts(Ignore = true), Authorize, Route("terms"), HttpPost, ValidateHttpAntiForgeryToken, ValidateInput(false)]
        public ActionResult Terms(TermsModel model)
        {
            if (model == null)
                return RedirectToAction("terms");

            var resource = Company.GlobalReportingResources.FirstOrDefault(r => r.ResourceID == Company.CurrentResourceID);
            List<OrganizationInvitation> invites = new List<OrganizationInvitation>();
            List<OrganizationResource> orgResources = new List<OrganizationResource>();


            if (resource != null)
            {
                invites = Company.OrganizationInvitations.Where(i => i.Email == resource.Email).ToList();
                orgResources = Company.OrganizationResources.Where(o => o.ResourceID == Company.CurrentResourceID).ToList();
            }

            var contract = model.Contract;
            var acceptance = model.Acceptance;
            var isLastContract = model.IsLastContract;


            if (contract.OrganizationID.HasValue)
            {

                var invite = invites.FirstOrDefault(i => i.OrganizationID == contract.OrganizationID);
                var orgRes = orgResources.FirstOrDefault(o => o.OrganizationID == contract.OrganizationID);

                if (orgRes == null && contract.OrganizationID != null)
                {
                    //add org resource record
                    orgRes = new OrganizationResource
                    {
                        ResourceID = Company.CurrentResourceID,
                        OrganizationID = (int)contract.OrganizationID,
                        Accepted = isLastContract,
                        DateAccepted = DateTime.UtcNow,

                    };

                    Company.Add(orgRes);
                }
                else
                {
                    orgRes.Accepted = isLastContract;
                    orgRes.DateAccepted = DateTime.UtcNow;
                    Company.Update(orgRes);
                }

                if (contract.ContractType == ContractType.OrganizationTermsOfUse)
                {
                    var org = Company.GetById<Organization>((int)contract.OrganizationID);
                    if (org != null && model.IsLastOrgContract)
                    {
                        org.Accepted = true;
                        org.AcceptedBy = Company.CurrentResourceID;
                        org.DateAccepted = DateTime.UtcNow;
                        Company.Update(org);
                    }
                }

                if (invite != null)
                    Company.Delete(invite); //remove the invite
            }
            else
            {
                if (isLastContract) //special case for default contract only on invite of existing user from different org
                {
                    invites.ForEach(i =>
                    {
                        var res = Company.GlobalReportingResources.FirstOrDefault(r => r.Email == i.Email);
                        if (res != null)
                        {
                            var oRes = Company.OrganizationResources.FirstOrDefault(o => i.OrganizationID == o.OrganizationID && o.ResourceID == res.ResourceID);
                            if (oRes == null)
                            {
                                Company.OrganizationResources.Add(new OrganizationResource
                                {
                                    ResourceID = res.ResourceID,
                                    OrganizationID = i.OrganizationID,
                                    Accepted = true,
                                    DateAccepted = DateTime.UtcNow,
                                });
                                Company.OrganizationInvitations.Remove(i);
                            }
                        }
                    });

                    Company.SaveChanges();
                }

                var orgRes = Company.OrganizationResources.Where(i => i.ResourceID == Company.CurrentResourceID).ToList();
                orgRes.ForEach(o =>
                {
                    o.Accepted = isLastContract;
                    o.DateAccepted = DateTime.UtcNow;
                    Company.Update(o);
                });

                if (contract.ContractType == ContractType.OrganizationTermsOfUse) //default org TOU, need to update each org user is a member of
                    orgRes.ForEach(o =>
                    {
                        if (model.OrgsWithContracts.Contains(o.OrganizationID))
                            return;

                        var org = Company.GetById<Organization>(o.OrganizationID);
                        if (org != null)
                        {
                            org.Accepted = true;
                            org.DateAccepted = DateTime.UtcNow;
                            org.AcceptedBy = Company.CurrentResourceID;
                            Company.Update(org);
                        }
                    });
            }

            acceptance.ContractID = contract.ID;
            acceptance.OrganizationID = contract.OrganizationID;
            acceptance.Accepted = true;
            acceptance.AcceptedOn = DateTime.UtcNow;
            acceptance.ResourceID = Company.CurrentResourceID;

            Company.Add(acceptance);

            return RedirectToAction("terms", new { redirectUri = model.RedirectUri });
        }

        /// <summary>
        /// Fallback for incorrect API URLs
        /// </summary>
        /// <returns></returns>
        [ValidateContracts, Authorize]
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            return Json(
                new {
                    title = "Error",
                    message = "The requested URL was not found. Please check the URL and all parameters are correct."
                }, 
                JsonRequestBehavior.AllowGet);
        }

        private bool updateContractValidationCache()
        {
            var key = ContractValidationCacheModel.cacheKey;
            var time = ContractValidationCacheModel.cacheDuration;
            var cacheResources = Cache.GetItem<ConcurrentBag<ContractValidationCacheModel.User>>(key);
            ContractValidationCacheModel.User cacheRes = null;
            var contractCount = Company.Query<int>(@"select count(*) from dbo.GetContractValidations(@ResourceID) where accepted = 0 and ((contractType = 1 and isFirstUser = 1) or contractType = 2 or organizationId is null)", new { ResourceID = Company.CurrentResourceID }).FirstOrDefault();
            var contractsAccepted = contractCount == 0;

            if (cacheResources != null)
            {
                cacheRes = cacheResources.FirstOrDefault(r => r.ID == Company.CurrentResourceID);
            }

            if (cacheRes != null)
            {
                var com = cacheRes.Companies.FirstOrDefault(c => c.ID == Company.CurrentCompanyID);
                if (com != null)
                {
                    if (!com.ContractsAccepted)
                    {
                        contractsAccepted = contractCount == 0;
                        com.ContractsAccepted = contractCount == 0;

                        Cache.SetItem(key, cacheResources, true, time);
                    }
                }
                else
                {
                    cacheRes.Companies.Add(new ContractValidationCacheModel.Company() { ID = Company.CurrentCompanyID, ContractsAccepted = contractCount == 0 });

                    Cache.SetItem(key, cacheResources, true, time);
                }
            }
            else
            {
                cacheRes = new ContractValidationCacheModel.User();
                cacheRes.Companies.Add(new ContractValidationCacheModel.Company() { ID = Company.CurrentCompanyID, ContractsAccepted = contractCount == 0 });
                Cache.SetItem(key, cacheResources, true, time);

            }

            return contractsAccepted;
        }

    }
}
