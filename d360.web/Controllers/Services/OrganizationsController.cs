using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core.entities;
using d360.core.enums;

using Microsoft.Web.Http;

namespace d360.web.Controllers.Services
{
	/// <summary>
	/// Manage your organizations within Data360.
	/// </summary>
	[ApiVersionNeutral, RoutePrefix("services/organizations"), ApiExplorerSettings(IgnoreApi = true), Authorize]
	public class OrganizationsController : BaseApiController
	{
		#region DI

		public OrganizationsController(CoreComponentSet set) : base(set)
		{
		}

		#endregion

		/// <summary>
		/// Gets a list of all organizations.
		/// </summary>
		/// <returns></returns>
		[HttpGet, Route("types")]
		public IEnumerable<OrganizationTypeDetail> GetOrganizationTypes()
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
			}

			return Company.Query<OrganizationTypeDetail>(@"
														select		T.ID,
																	T.Name,
																	T.Description,
																	A.ID as AssetTypeID,
																	(select count(1) from organization o where organizationtypeid = t.id and o.state = 1) as OrganizationCount,
																	A.uid
														from		OrganizationType T
																	inner join AssetType A on A.Object = 'OrganizationType' and A.ObjectID = T.ID
														where       T.State = 1
														order by    T.Name");
		}

		/// <summary>
		/// Gets a list of all organizations.
		/// </summary>
		/// <returns></returns>
		[HttpGet, Route("{id:int}/items")]
		public IQueryable<OrganizationDetail> GetOrganizationsByType(int id)
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return null;
			}

			return Company.OrganizationDetails.Where(x => x.OrganizationTypeID == id);
		}

		/// <summary>
		/// Gets a list of all default contracts.
		/// </summary>
		/// <returns>Reutnrs a list of all contracts that are not specifically tied to any one organization, instead acting as a default contract for all organizations.</returns>
		[HttpGet, Route("default/contracts")]
		public IEnumerable<ContractDetail> GetDefaultContracts()
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
			}

			var contractTypes = ContractType.OrganizationTermsOfUse.GetEnumList();

			return Company.Query<ContractDetail>(@"select 
					c.ID, 
					c.Title, 
					c.Body, 
					c.OrganizationID, 
					c.ContractType, 
					c.PublishedOn, 
					c.UpdatedOn, 
					c.UpdatedBy, 
					coalesce(O.Name, 'Default') as OrganizationName
					from [Contract] c
					left join Organization o on o.ID = c.OrganizationID
					where c.State <> 3 and c.OrganizationID is null
					order by c.Title asc")
				.Select(c =>
				{
					var ct = contractTypes.Find(t => t.ID == c.ContractType);
					c.ContractTypeName = ct.Name;
					c.ContractTypeDescription = ct.Description;
					return c;
				});
		}

		/// <summary>
		/// Gets a list of all contracts for a specified organization.
		/// </summary>
		/// <param name="id">The ID of the organization you want to retrieve contracts for.</param>
		/// <returns></returns>
		[HttpGet, Route("{id:int}/contracts")]
		public IEnumerable<ContractDetail> GetContractsByOrganization(int id)
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return null;
			}

			var contractTypes = ContractType.OrganizationTermsOfUse.GetEnumList();

			return Company.Query<ContractDetail>(@"select 
					c.ID, 
					c.Title, 
					c.Body, 
					c.OrganizationID, 
					c.ContractType, 
					c.PublishedOn, 
					c.UpdatedOn, 
					c.UpdatedBy, 
					coalesce(O.Name, 'Default') as OrganizationName
					from [Contract] c
					left join Organization o on o.ID = c.OrganizationID
					where c.State <> 3 and c.OrganizationID = @id
					order by c.Title asc", new { id })
				.Select(c =>
				{
					var ct = contractTypes.Find(t => t.ID == c.ContractType);
					c.ContractTypeName = ct.Name;
					c.ContractTypeDescription = ct.Description;
					return c;
				});
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
			{
				return null;
			}

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
			{
				return null;
			}

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
			{
				return null;
			}

			var sql = @"select o.* from OrganizationResourceDetail o
							inner join reporting.Global_Resource r on
							r.ResourceID = o.ResourceID
							where r.State =@userStatus and o.OrganizationID=@orgId";

			return Company.Query<OrganizationResourceDetail>(sql, new { userStatus = CompanyResourceState.Active, orgId = id }).AsQueryable();
		}

		/// <summary>
		/// Gets a history of contract acceptance for the resource
		/// </summary>
		/// <param name="id">The ID of the resource you want to retrieve history for.</param>
		/// <returns></returns>
		[HttpGet, Route("history/resource/{id:int}")]
		public IQueryable<ContractAcceptanceDetail> GetContractHistoryForResource(int id)
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return null;
			}

			return Company.Query<ContractAcceptanceDetail>(@"select h.*, r.FirstName + ' ' + r.LastName as ResourceName, c.Title as ContractName from contractacceptance h
				inner join reporting.Global_resource r on r.ResourceID = h.ResourceID
				inner join [Contract] c on c.id = h.ContractID
				where h.ResourceID = @id", new { id }).AsQueryable().OrderByDescending(c => c.AcceptedOn);
		}

		/// <summary>
		/// Gets a history of contract acceptance for the contract
		/// </summary>
		/// <param name="id">The ID of the contract you want to retrieve history for.</param>
		/// <returns></returns>
		[HttpGet, Route("history/contract/{id:int}")]
		public IQueryable<ContractAcceptanceDetail> GetContractHistoryForContract(int id)
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return null;
			}

			return Company.Query<ContractAcceptanceDetail>(@"select h.*, r.FirstName + ' ' + r.LastName as ResourceName, c.Title as ContractName from contractacceptance h
				inner join reporting.Global_resource r on r.ResourceID = h.ResourceID
				inner join [Contract] c on c.id = h.ContractID
				where h.ContractID = @id", new { id }).AsQueryable().OrderByDescending(c => c.AcceptedOn);
		}

		/// <summary>
		/// Gets a history of contract acceptance for the organization
		/// </summary>
		/// <param name="id">The ID of the organization you want to retrieve history for.</param>
		/// <returns></returns>
		[HttpGet, Route("history/organization/{id:int}")]
		public IQueryable<ContractAcceptanceDetail> GetContractHistoryForOrganization(int id)
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return null;
			}

			return Company.Query<ContractAcceptanceDetail>(@"select h.*, r.FirstName + ' ' + r.LastName as ResourceName, c.Title as ContractName from contractacceptance h
				inner join reporting.Global_resource r on r.ResourceID = h.ResourceID
				inner join [Contract] c on c.id = h.ContractID
				where h.OrganizationID = @id", new { id }).AsQueryable().OrderByDescending(c => c.AcceptedOn);
		}
	}
}
