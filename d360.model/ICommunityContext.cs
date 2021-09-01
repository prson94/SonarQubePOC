using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.enums;

namespace d360.model
{
    public interface ICommunityContext : IBaseContext
    {
        DbSet<Client> Clients { get; set; }
        DbSet<Company> Companies { get; set; }
        DbSet<CompanyDomainGroup> CompanyDomainGroups { get; set; }
        DbSet<CompanyDomainSetting> CompanyDomainSettings { get; set; }        
        DbSet<CompanyRebuildJobStatus> CompanyRebuildJobStatuses { get; set; }        
        DbSet<CompanyResource> CompanyResources { get; set; }
        CompanySsoModel CurrentCompanySsoModel { get; set; }
        DbSet<DatabaseServer> DatabaseServers { get; set; }
        DbSet<DomainCertificate> DomainCertificates { get; set; }
        DbSet<DomainSetting> DomainSettings { get; set; }                        
        DbSet<Resource> Resources { get; set; }

        new bool Add<T>(T item) where T : BaseObject;
        bool ChangePassword(int resourceID, string oldPassword, string newPassword);        
        new bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        new bool Delete<T>(T entity) where T : BaseObject;
        string GetCompanyConnectionString(bool skipCacheCheck = false);
        //Dictionary<string, string> GetCompanySettings();
        string GetPrimaryUrlPrefix();
        //T GetCompanySettingByKey<T>(string key);
        Task<List<CompanyRebuildJobStatus>> GetRebuildJobStatuses();
        Task<CompanyRebuildJobStatusState> GetRebuildJobStatus(CompanyRebuildJobToken jobToken);
        Task<WorkHttpStatus> UpdateRebuildJobStatus(CompanyRebuildJobToken jobToken, CompanyRebuildJobStatusState state);        
        IEnumerable<T> Query<T>(string sql, object param = null);
        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null);
        int SaveChanges();
        new bool Update<T>(T item) where T : BaseObject;
        Resource ValidateResource(string username, string password);
    }
}