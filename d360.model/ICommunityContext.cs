using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq.Expressions;
using d360.core.entities;
using d360.core.entities.Plugins;

namespace d360.model
{
    public interface ICommunityContext : IBaseContext
    {
        DbSet<Client> Clients { get; set; }
        DbSet<Company> Companies { get; set; }
        DbSet<CompanyDomainSetting> CompanyDomainSettings { get; set; }
        DbSet<CompanyFeature> CompanyFeatures { get; set; }
        DbSet<CompanyHelpResource> CompanyHelpResources { get; set; }
        DbSet<CompanyResource> CompanyResources { get; set; }
        DbSet<CompanySetting> CompanySettings { get; set; }
        CompanySsoModel CurrentCompanySsoModel { get; set; }
        DbSet<DatabaseServer> DatabaseServers { get; set; }
        DbSet<DomainCertificate> DomainCertificates { get; set; }
        DbSet<DomainSetting> DomainSettings { get; set; }
        DbSet<EventType> EventTypes { get; set; }
        DbSet<FusionAttributeTypeField> FusionAttributeTypeFields { get; set; }
        DbSet<core.entities.Plugins.FusionAttributeType> FusionAttributeTypes { get; set; }
        DbSet<FusionIntersectType> FusionIntersectTypes { get; set; }
        DbSet<core.entities.Plugins.FusionType> FusionTypes { get; set; }
        DbSet<HelpResource> HelpResources { get; set; }
        DbSet<PackageContent> PackageContents { get; set; }
        DbSet<Package> Packages { get; set; }
        DbSet<Resource> Resources { get; set; }
        DbSet<ResourceType> ResourceTypes { get; set; }
        DbSet<Setting> Settings { get; set; }

        bool Add<T>(T item) where T : BaseObject;
        bool ChangePassword(int resourceID, string oldPassword, string newPassword);
        string createRandomPassword();
        bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        bool Delete<T>(T entity) where T : BaseObject;
        string GetCompanyConnectionString(bool skipCacheCheck = false);
        Dictionary<string, string> GetCompanySettings();
        string HashPassword(string value);
        IEnumerable<T> Query<T>(string sql, object param = null);
        int SaveChanges();
        bool Update<T>(T item) where T : BaseObject;
        Resource ValidateResource(string username, string password);
    }
}