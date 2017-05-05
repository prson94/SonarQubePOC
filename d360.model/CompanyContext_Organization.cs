using d360.core;
using d360.core.entities;
using Dapper;
using System;
using System.Data.Entity;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<Contract> Contracts { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<OrganizationDomain> OrganizationDomains { get; set; }

        public DbSet<OrganizationInvitation> OrganizationInvitations { get; set; }

        public DbSet<OrganizationResource> OrganizationResources { get; set; }

        #endregion

        #region Engine Methods

        #endregion
    }
}
