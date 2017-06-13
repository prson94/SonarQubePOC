using d360.core.entities;
using d360.core.enums;
using d360.model;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// Manage your organizations within Data3Sixty.
    /// </summary>
    [RoutePrefix("services/organizations"), Authorize]
    public class OrganizationsController : BaseApiController
    {
        #region DI

        public OrganizationsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        /// <summary>
        /// Gets a list of all organizations.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("")]
        public IQueryable<OrganizationDetail> GetOrganizations()
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return Company.Table<OrganizationDetail>();
        }

        /// <summary>
        /// Gets a list of all default contracts.
        /// </summary>
        /// <returns>Reutnrs a list of all contracts that are not specifically tied to any one organization, instead acting as a default contract for all organizations.</returns>
        [HttpGet, Route("default/contracts")]
        public IEnumerable<ContractModel> GetDefaultContracts()
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return (
                    from c in Company.Contracts.ToList()
                    join ct in ContractType.OrganizationTermsOfUse.GetEnumList() on c.ContractType equals ct.ID
                    where !c.OrganizationID.HasValue
                    select new ContractModel
                    {
                        Body = c.Body,
                        ContractType = c.ContractType,
                        ContractTypeDescription = ct.Description,
                        ContractTypeName = ct.Name,
                        ID = c.ID,
                        OrganizationID = c.OrganizationID,
                        OrganizationName = "Default",
                        Title = c.Title
                    }
                    );
        }

        /// <summary>
        /// Gets a list of all contracts for a specified organization.
        /// </summary>
        /// <param name="id">The ID of the organization you want to retrieve contracts for.</param>
        /// <returns></returns>
        [HttpGet, Route("{id:int}/contracts")]
        public IEnumerable<ContractModel> GetContractsByOrganization(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return (
                    from c in Company.Contracts.Include("Organization").ToList()
                    join ct in ContractType.OrganizationTermsOfUse.GetEnumList() on c.ContractType equals ct.ID
                    where c.OrganizationID == id
                    select new ContractModel {
                        Body = c.Body,
                        ContractType = c.ContractType,
                        ContractTypeDescription = ct.Description,
                        ContractTypeName = ct.Name,
                        ID = c.ID,
                        OrganizationID = c.OrganizationID,
                        OrganizationName = (c.Organization != null) ? c.Organization.Name : "Global",
                        Title = c.Title
                    }
                    );
        }

        /// <summary>
        /// Gets a list of all domains for a specified organization.
        /// </summary>
        /// <param name="id">The ID of the organization you want to retrieve domains for.</param>
        /// <returns></returns>
        [HttpGet, Route("{id:int}/domains")]
        public IQueryable<OrganizationDomain> GetDomainsByOrganization(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return Company.Filter<OrganizationDomain>(i => i.OrganizationID == id);
        }

        /// <summary>
        /// Gets a list of all invitations for a specified organization.
        /// </summary>
        /// <param name="id">The ID of the organization you want to retrieve invitations for.</param>
        /// <returns></returns>
        [HttpGet, Route("{id:int}/invitations")]
        public IQueryable<OrganizationInvitationDetail> GetInvitationsByOrganization(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return Company.Filter<OrganizationInvitationDetail>(i => i.OrganizationID == id);
        }

        /// <summary>
        /// Gets a list of all users for a specified organization.
        /// </summary>
        /// <param name="id">The ID of the organization you want to retrieve users for.</param>
        /// <returns></returns>
        [HttpGet, Route("{id:int}/users")]
        public IQueryable<OrganizationResourceDetail> GetResourcesByOrganization(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return Company.Filter<OrganizationResourceDetail>(i => i.OrganizationID == id);
        }

        /// <summary>
        /// Gets a list of all domains across all organizations.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("domains")]
        public IQueryable<OrganizationDomain> GetDomainsForAllOrganizations()
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return Company.Table<OrganizationDomain>();
        }

        /// <summary>
        /// Gets a list of all invitations across all organizations.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("invitations")]
        public IQueryable<OrganizationInvitationDetail> GetInvitationsForAllOrganizations()
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return Company.Table<OrganizationInvitationDetail>();
        }

        /// <summary>
        /// Gets a list of all users across all organizations.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("users")]
        public IQueryable<OrganizationResourceDetail> GetResourcesForAllOrganizations()
        {
            if (!Company.CurrentResourceIsAdmin)
                return null;

            return Company.Table<OrganizationResourceDetail>();
        }
    }
}
