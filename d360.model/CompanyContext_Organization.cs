using System.Data.Entity;

using d360.core.entities;

namespace d360.model
{
    public partial class CompanyContext : BaseContext
    {
        #region DbSets

        public DbSet<Contract> Contracts { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<OrganizationDetail> OrganizationDetails { get; set; }

        public DbSet<OrganizationDomain> OrganizationDomains { get; set; }

        public DbSet<OrganizationInvitation> OrganizationInvitations { get; set; }

        public DbSet<OrganizationInvitationDetail> OrganizationInvitationDetails { get; set; }

        public DbSet<OrganizationRegistration> OrganizationRegistrations { get; set; }

        public DbSet<OrganizationResource> OrganizationResources { get; set; }

        public DbSet<OrganizationResourceDetail> OrganizationResourceDetails { get; set; }

        public DbSet<OrganizationType> OrganizationTypes { get; set; }

        #endregion
    }
}
