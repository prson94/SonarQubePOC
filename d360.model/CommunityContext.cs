using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.extensions;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace d360.model
{
    [DbConfigurationType(typeof(AzureConfiguration))]
    public class CommunityContext : BaseContext, ICommunityContext
    {
        internal IQueueSource QueueSource;
        internal string SettingsCacheKey;

        public CompanySsoModel CurrentCompanySsoModel { get; set; }

        public CommunityContext(ICachingProvider caching, IQueueSource queueSource, ISecurityContextProvider context)
            : base(constants.COMMUNITY_DATABASE_CONNECTION)
        {
            Database.SetInitializer<CommunityContext>(null); //dont create any tables if they dont exist.

            Caching = caching;
            QueueSource = queueSource;

            CurrentCompanyID = context.CompanyID;
            CurrentDomainSettingID = context.DomainSettingID;
            CurrentResourceID = context.ResourceID;
            CurrentResourceIsAdmin = context.IsAdministrator;
            CurrentCompanyDomain = context.CompanyPrefix;
            GetCompanySsoModel();

            SettingsCacheKey = $"c{CurrentCompanyID}_settings";
        }


        #region DbSets

        public DbSet<Client> Clients { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyDomainGroup> CompanyDomainGroups { get; set; }
        public DbSet<CompanyDomainSetting> CompanyDomainSettings { get; set; }        
        public DbSet<CompanyRebuildJobStatus> CompanyRebuildJobStatuses { get; set; }
        public DbSet<CompanyResource> CompanyResources { get; set; }
        public DbSet<DatabaseServer> DatabaseServers { get; set; }
        public DbSet<DomainCertificate> DomainCertificates { get; set; }
        public DbSet<DomainSetting> DomainSettings { get; set; }        
        public DbSet<Resource> Resources { get; set; }
        
        #endregion

        #region Generic methods

        public override bool Add<T>(T item)
        {
            Set<T>().Add(item);

            if (item is Resource || item is CompanyResource)
            {
                Caching.RemoveItem("Users");
                Caching.RemoveItem("RESOURCES");
            }

            return (SaveChanges() > 0);
        }

        public override bool Delete<T>(Expression<Func<T, bool>> predicate)
        {
            var items = Filter(predicate).ToList();
            bool allDeleted = true;
            bool clearCache = false;


            items.ForEach(i =>
            {
                if (i is CompanyResource) clearCache = true;
                if (i is Resource) clearCache = true;

                if (!Delete(i))
                {
                    allDeleted = false;
                }
            });

            SaveChanges();

            if (clearCache)
            {
                Caching.RemoveItem("Users");
                Caching.RemoveItem("RESOURCES");  
            }

            return allDeleted;
        }

        public override bool Delete<T>(T entity)
        {
            try
            {
                Set<T>().Remove(entity);
                SaveChanges();

                if (entity is Resource || entity is CompanyResource)
                {
                    Caching.RemoveItem("Users");
                    Caching.RemoveItem("RESOURCES");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;                
            }
        }

        public IEnumerable<T> Query<T>(string sql, object param = null)
        {
            return Database.Connection.Query<T>(sql, param);
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null)
        {
            return await (Database.Connection.QueryFirstOrDefaultAsync<T>(sql, param));
        }

        public override int SaveChanges()
        {
            int returnValue = 0;
            bool bClearSettingsCache = false;

            var changedItems = this.ChangeTracker.Entries().Where(p => p.State == EntityState.Added ||
                    p.State == EntityState.Deleted ||
                    p.State == EntityState.Modified);
           
            try
            {
                returnValue = base.SaveChanges();
            }
            catch (OptimisticConcurrencyException)
            {
            }
     
            return returnValue;
        }

        public override bool Update<T>(T item)
        {
            if (item is Resource || item is CompanyResource)
            {
                Caching.RemoveItem("Users");
                Caching.RemoveItem("RESOURCES");
            }

            this.ChangeTracker.DetectChanges();
            return  (SaveChanges() > 0);            
        }

        #endregion

        public async Task<List<CompanyRebuildJobStatus>> GetRebuildJobStatuses()
        {
            int timeoutInHours = 18;
            if (int.TryParse(constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS, out int timeout))
            {
                timeoutInHours = timeout;
            }
            var list = await CompanyRebuildJobStatuses.Where(j => j.CompanyID == this.CurrentCompanyID).ToListAsync();
            list.ForEach(i => {
                if (i.State == CompanyRebuildJobStatusState.Active && i.LastStartedOn <= DateTime.UtcNow.AddHours(-timeoutInHours))
                {
                    i.State = CompanyRebuildJobStatusState.Inactive;
                }
            });
            return list;
        }

        public async Task<CompanyRebuildJobStatusState> GetRebuildJobStatus(CompanyRebuildJobToken jobToken)
        {
            int timeoutInHours = 18;
            if (int.TryParse(constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS, out int timeout))
            {
                timeoutInHours = timeout;
            }
            var status = await CompanyRebuildJobStatuses.FirstOrDefaultAsync(j => j.CompanyID == this.CurrentCompanyID && j.JobToken == jobToken);
            CompanyRebuildJobStatusState state = CompanyRebuildJobStatusState.Inactive;
            if (status != null && status.LastStartedOn > DateTime.UtcNow.AddHours(-timeoutInHours))
            {
                state = status.State;
            }
            return state;
        }

        public async Task<WorkHttpStatus> UpdateRebuildJobStatus(CompanyRebuildJobToken jobToken, CompanyRebuildJobStatusState state)
        {
            int timeoutInHours = 18;
            if (int.TryParse(constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS, out int timeout))
            {
                timeoutInHours = timeout;
            }
            var status = await CompanyRebuildJobStatuses.FirstOrDefaultAsync(j => j.CompanyID == this.CurrentCompanyID && j.JobToken == jobToken);
            WorkHttpStatus returnValue = null;

            if (status != null)
            {
                if (status.State == CompanyRebuildJobStatusState.Active && status.LastStartedOn > DateTime.UtcNow.AddHours(-timeoutInHours) && state == CompanyRebuildJobStatusState.Active)
                {
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.Conflict, "Job is currently running", $"This job is currently in an Active state and cannot be scheduled again until complete.");
                }
                else
                {
                    status.State = state;
                    if (state == CompanyRebuildJobStatusState.Active)
                    {
                        status.LastStartedOn = DateTime.UtcNow;
                        status.LastCompletedOn = null;
                    }
                    else 
                    {
                        status.LastCompletedOn = DateTime.UtcNow;
                    }
                    Update(status);
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.OK, "", "");
                }
            }
            else 
            {
                if (state == CompanyRebuildJobStatusState.Inactive)
                {
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.Conflict, "Job is not currently running", $"This job is not currently running and cannot be marked as complete.");
                }
                else 
                {
                    status = new CompanyRebuildJobStatus { CompanyID = CurrentCompanyID, JobToken = jobToken, LastStartedBy = CurrentResourceID, LastStartedOn = DateTime.UtcNow, State = state };
                    Add(status);
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.OK, "", "");
                }
            }
            
            return returnValue;
        }

        void GetCompanySsoModel()
        {
            if (Caching.ListItemExists<CompanySsoModel, string>(CACHE_KEY_SSO_MODELS, CurrentCompanyDomain))
            {
                CurrentCompanySsoModel = Caching.GetItemInListByID<CompanySsoModel, string>(CACHE_KEY_SSO_MODELS,
                    CurrentCompanyDomain);
            }
            else
            {
                CurrentCompanySsoModel = new CompanySsoModel();

                var model = (
                            from c in Companies
                            from cds in c.CompanyDomainSettings
                            where c.ID == CurrentCompanyID
                            where cds.UrlPrefix == CurrentCompanyDomain
                            select new
                            {
                                cds.AllowNewUserLogin,
                                cds.AuthenticationType,
                                cds.DomainSetting.HashAlgorithmType,
                                cds.DomainSetting.IdpSloEndpoint,
                                cds.DomainSetting.IdpSsoEndpoint,
                                cds.DomainSetting.IdpDomainCertificate,
                                cds.DomainSetting.SpDomainCertificate,
                                cds.DomainSetting.SignInitialSSORequest,
                                c.Status
                            }
                            ).SingleOrDefault();
                if (model != null)
                {
                    CurrentCompanySsoModel.AllowNewUserLogin = model.AllowNewUserLogin;
                    CurrentCompanySsoModel.AuthenticationType = model.AuthenticationType;
                    CurrentCompanySsoModel.IdpSloEndpoint = model.IdpSloEndpoint;
                    CurrentCompanySsoModel.IdpSsoEndpoint = model.IdpSsoEndpoint;
                    CurrentCompanySsoModel.HashAlgorithmType = model.HashAlgorithmType;
                    CurrentCompanySsoModel.SignInitialSSORequest = model.SignInitialSSORequest;
                    CurrentCompanySsoModel.IsCompanyActive = model.Status != null && model.Status.ToLower() == "active" ? true : false;

                    if (model.IdpDomainCertificate != null)
                    {
                        CurrentCompanySsoModel.IdpCertificateFile = model.IdpDomainCertificate.File;
                        CurrentCompanySsoModel.IdpCertificatePassword = model.IdpDomainCertificate.Password;
                    }
                    if (model.SpDomainCertificate != null)
                    {
                        CurrentCompanySsoModel.SpCertificateFile = model.SpDomainCertificate.File;
                        CurrentCompanySsoModel.SpCertificatePassword = model.SpDomainCertificate.Password;
                    }
                }

                Caching.SetItemInListByID<CompanySsoModel, string>(CACHE_KEY_SSO_MODELS, CurrentCompanyDomain, CurrentCompanySsoModel);
            }
        }

        public string GetCompanyConnectionString(bool skipCacheCheck = false)
        {
            string cs;

            if (Caching.ListItemExists<string, int>(CACHE_KEY_CONNECTION_STRINGS, CurrentCompanyID) && !skipCacheCheck)
            {
                cs = Caching.GetItemInListByID<string, int>(CACHE_KEY_CONNECTION_STRINGS, CurrentCompanyID);
                return cs;
            }
            else
            {                
                var res = Database.Connection.QuerySingle(@"select s.Server, s.Username, s.Password from Company c
                                inner join DatabaseServer s on s.ID = c.DatabaseServerID 
                                where c.ID = @companyId", new { companyId = CurrentCompanyID });

                cs = CompanyConnectionStringHelper.ConnectionString(CurrentCompanyID, res.Server, res.Username, res.Password);
                
                if (!skipCacheCheck)
                {
                    Caching.SetItemInListByID<string, int>(CACHE_KEY_CONNECTION_STRINGS, CurrentCompanyID, cs);
                }

                return cs;
            }
        }

        public bool ChangePassword(int resourceID, string oldPassword, string newPassword)
        {
            var success = false;

            try
            {
                if (oldPassword != newPassword)
                {
                    var r = GetById<Resource>(resourceID);
                    if (r != null)
                    {
                        r.Password = PasswordHelper.HashPassword(newPassword);
                        r.UpdatedOn = DateTime.UtcNow;
                        Update<Resource>(r);
                        success = true;
                    }
                    r = null;
                }
                else
                {
                    throw new ApplicationException("New password may not be the same as old password.");
                }
            }
            catch
            {
                throw;
            }

            return success;
        }

        public Resource ValidateResource(string username, string password)
        {
            Resource r = null;

            password = PasswordHelper.HashPassword(password);
            r = Filter<Resource>(i => i.Username == username && i.Password == password).SingleOrDefault();

            // Check that resource has access to this company.
            if (r != null)
            {
                var companyResource = Filter<CompanyResource>(i => i.CompanyID == CurrentCompanyID && i.ResourceID == r.ID).SingleOrDefault();
                if (companyResource != null)
                {
                    if (companyResource.State == CompanyResourceState.Active)
                    {
                        companyResource.LastLoggedInOn = DateTime.UtcNow;
                        Update(companyResource);
                    }
                    else // User is NOT active, so do not allow login.
                    {
                        r = null;
                    }
                }
                else
                {
                    r = null;
                }
            }

            return r;
        }

        public class SettingModel
        {
            public int SettingID { get; set; }

            public string Name { get; set; }

            public string FieldName { get; set; }

            public string Description { get; set; }

            public string Value { get; set; }
        }

        public T GetCompanySettingByKey<T>(string key)
        {
            var settings = GetCompanySettings();
            if (settings.ContainsKey(key))
            {
                var value = settings[key];
                try
                {
                    T checkType = default(T);
                    if (checkType is Guid)
                    {
                        var guid = Guid.Parse(value);
                        return (T)(Convert.ChangeType(guid, typeof(T)));
                    }

                    return (T)(Convert.ChangeType(value, typeof(T)));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else throw new Exception($"Invalid settings key '{key}'");
        }


        public string GetPrimaryUrlPrefix()
        {
            return Query<string>(@"select UrlPrefix from CompanyDomainSetting where CompanyID = @c and IsPrimary = 1", new { c = CurrentCompanyID }).FirstOrDefault();
        }

        public Dictionary<string, string> GetCompanySettings()
        {
            Dictionary<string, string> settings = Caching.GetItem<Dictionary<string, string>>(SettingsCacheKey);

            if (settings == null)
            {
                settings = Query<SettingModel>(
    @"select S.ID as SettingID, S.Name, S.FieldName, S.Description, coalesce(C.Value, S.DefaultValue) as Value
from Setting S left join CompanySetting C on C.SettingID = S.ID and C.CompanyID = @c
where S.ID <> 4", new { c = CurrentCompanyID })
    .ToDictionary(k => k.FieldName, v => v.Value);

                Caching.SetItem(SettingsCacheKey, settings, false, 10);
            }
            return settings;
        }

    }
}
