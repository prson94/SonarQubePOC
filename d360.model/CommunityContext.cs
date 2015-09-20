using System.Linq;
using System.Collections.Generic;
using System.Data.Entity;
using d360.core.entities;
using d360.extensions;
using d360.core;
using System;
using System.Linq.Expressions;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core;
using Dapper;
using System.Security.Cryptography;
using System.Text;

namespace d360.model
{
    [DbConfigurationType(typeof(AzureConfiguration))]
    public class CommunityContext : BaseContext
    {
        internal IQueueSource QueueSource;

        public CompanySsoModel CurrentCompanySsoModel { get; set; }

        public CommunityContext(ICachingProvider caching, IQueueSource queueSource, ISecurityContextProvider context)
            : base(constants.COMMUNITY_DATABASE_CONNECTION)
        {
            Context = context;
            Caching = caching;
            QueueSource = queueSource;

            CurrentCompanyID = GetCompanyID();
            CurrentResourceID = GetResourceID();
            CurrentResourceIsAdmin = GetResourceAdminFlag();
            GetCompanySsoModel();
        }

        #region DbSets

        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyDomainSetting> CompanyDomainSettings { get; set; }
        public DbSet<CompanyFeature> CompanyFeatures { get; set; }
        public DbSet<CompanyResource> CompanyResources { get; set; }
        public DbSet<CompanySetting> CompanySettings { get; set; }
        public DbSet<DatabaseServer> DatabaseServers { get; set; }
        public DbSet<DomainCertificate> DomainCertificates { get; set; }
        public DbSet<DomainSetting> DomainSettings { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<ResourceType> ResourceTypes { get; set; }
        public DbSet<Setting> Settings { get; set; }

        public DbSet<JobActivity> JobActivities { get; set; }

        public DbSet<d360.core.entities.Plugins.Package> Packages { get; set; }
        public DbSet<d360.core.entities.Plugins.PackageContent> PackageContents { get; set; }

        public DbSet<d360.core.entities.Plugins.EventType> EventTypes { get; set; }
        public DbSet<d360.core.entities.Plugins.FieldType> FieldTypes { get; set; }
        public DbSet<d360.core.entities.Plugins.FusionAttributeType> FusionAttributeTypes { get; set; }
        public DbSet<d360.core.entities.Plugins.FusionIntersectType> FusionIntersectTypes { get; set; }
        public DbSet<d360.core.entities.Plugins.FusionType> FusionTypes { get; set; }

        #endregion

        #region Base overrides

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<d360.core.entities.Plugins.EventType>()
                .HasMany(x => x.FieldTypes)
                .WithMany(x => x.EventTypes)
                .Map(x =>
                {
                    x.ToTable("EventTypeFieldType", "plugin");
                    x.MapLeftKey("EventTypeID");
                    x.MapRightKey("FieldTypeID");
                });

            modelBuilder.Entity<d360.core.entities.Plugins.FusionAttributeType>()
                .HasMany(x => x.FieldTypes)
                .WithMany(x => x.FusionAttributeTypes)
                .Map(x =>
                {
                    x.ToTable("FusionAttributeTypeFieldType", "plugin");
                    x.MapLeftKey("FusionAttributeTypeID");
                    x.MapRightKey("FieldTypeID");
                });

            modelBuilder.Entity<d360.core.entities.Plugins.FusionType>()
                .HasMany(x => x.FieldTypes)
                .WithMany(x => x.FusionTypes)
                .Map(x =>
                {
                    x.ToTable("FusionTypeFieldType", "plugin");
                    x.MapLeftKey("FusionTypeID");
                    x.MapRightKey("FieldTypeID");
                });

            modelBuilder.Entity<d360.core.entities.Company>()
                .HasMany(x => x.Packages)
                .WithMany(x => x.Companies)
                .Map(x =>
                {
                    x.ToTable("CompanyPackage", "plugin");
                    x.MapLeftKey("CompanyID");
                    x.MapRightKey("PackageID");
                });

            //modelBuilder.Entity<EventHeader>().HasKey(p => new { p.EventGroupID });
        }

        #endregion

        #region Generic methods

        public override bool Add<T>(T item)
        {
            Set<T>().Add(item);

            if (item is Resource || item is CompanyResource)
            {
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
                    Caching.RemoveItem("RESOURCES");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
                //ex = resolveToRealException(ex);
                //throw new d360.core.exceptions.ConflictException(ex.Message, ex.Message);
            }
        }

        public IEnumerable<T> Query<T>(string sql, object param = null)
        {
            return Database.Connection.Query<T>(sql, param);
        }

        public override int SaveChanges()
        {
            int returnValue = 0;

            foreach (var entry in ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added | System.Data.Entity.EntityState.Modified | System.Data.Entity.EntityState.Deleted))
            {

            }
           
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
                Caching.RemoveItem("RESOURCES");
            }

            return (SaveChanges() > 0);
        }

        #endregion

        #region For Deriving company and resource records based on incoming raw values

        void GetCompanySsoModel()
        {
            if (Caching.ListItemExists<CompanySsoModel, string>(CACHE_KEY_SSO_MODELS, Context.RawCompanyID))
            {
                CurrentCompanySsoModel = Caching.GetItemInListByID<CompanySsoModel, string>(CACHE_KEY_SSO_MODELS, 
                    Context.RawCompanyID);
            }
            else
            {
                CurrentCompanySsoModel = new CompanySsoModel();

                switch (Context.CompanyIDType)
                {
                    case CompanyIdentifierType.Uri:
                        var model = (
                                    from c in Companies
                                    from cds in c.CompanyDomainSettings
                                    where c.ID == CurrentCompanyID
                                    where cds.UrlPrefix == Context.RawCompanyID
                                    select new
                                    {
                                        cds.AllowNewUserLogin,
                                        cds.AuthenticationType,
                                        cds.DomainSetting.HashAlgorithmType,
                                        cds.DomainSetting.IdpSloEndpoint,
                                        cds.DomainSetting.IdpSsoEndpoint,
                                        cds.DomainSetting.IdpDomainCertificate,
                                        cds.DomainSetting.SpDomainCertificate
                                    }
                                    ).SingleOrDefault();
                        if (model != null)
                        {
                            CurrentCompanySsoModel.AllowNewUserLogin = model.AllowNewUserLogin;
                            CurrentCompanySsoModel.AuthenticationType = model.AuthenticationType;
                            CurrentCompanySsoModel.IdpSloEndpoint = model.IdpSloEndpoint;
                            CurrentCompanySsoModel.IdpSsoEndpoint = model.IdpSsoEndpoint;
                            CurrentCompanySsoModel.HashAlgorithmType = model.HashAlgorithmType;

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
                        break;
                    default:
                        // do nothing.
                        break;
                }

                Caching.SetItemInListByID<CompanySsoModel, string>(CACHE_KEY_SSO_MODELS, Context.RawCompanyID, CurrentCompanySsoModel);
            }
        }

        public string GetCompanyConnectionString()
        {
            var cs = "";

            if (Caching.ListItemExists<string, int>(CACHE_KEY_CONNECTION_STRINGS, CurrentCompanyID))
            {
                cs = Caching.GetItemInListByID<string, int>(CACHE_KEY_CONNECTION_STRINGS, CurrentCompanyID);
                return cs;
            }
            else
            {
                var c = GetCompany();
                cs = string.Format(
                    "server={0};Database=D3S_{1};User ID={2};Password={3}",
                    c.DatabaseServer.Server,
                    c.ID,
                    c.DatabaseServer.Username,
                    c.DatabaseServer.Password
                );
                c = null;

                Caching.SetItemInListByID<string, int>(CACHE_KEY_CONNECTION_STRINGS, CurrentCompanyID, cs);

                return cs;
            }
        }

        internal override Company GetCompany()
        {
            Company c = null;

            switch (Context.CompanyIDType)
            {
                case CompanyIdentifierType.ID:
                    int iID;
                    if (int.TryParse(Context.RawCompanyID, out iID))
                    {
                        c = GetById<Company>(iID, i => i.DatabaseServer);
                    }
                    break;
                //case CompanyIdentifierType.PublicID:
                //    Guid gID;
                //    if (Guid.TryParse(Context.RawCompanyID, out gID))
                //    {
                //        c = Filter<Company>(i => i.PublicID == gID, i => i.DatabaseServer).SingleOrDefault();
                //    }
                //    break;
                case CompanyIdentifierType.Uri:
                    c = Filter<Company>(i => i.CompanyDomainSettings.Any(u => u.UrlPrefix == Context.RawCompanyID), i => i.DatabaseServer).SingleOrDefault();
                    break;
            }

            return c;
        }

        internal override Resource GetResource()
        {
            Resource r = null;

            switch (Context.UserIDType)
            {
                case UserIdentifierType.ApiKey:
                    r = Resources.SingleOrDefault(i => i.APIPublicKey == Context.RawUserID);
                    break;
                case UserIdentifierType.Email:
                    r = Resources.SingleOrDefault(i => i.Email == Context.RawUserID);
                    break;
                case UserIdentifierType.ID:
                    r = Resources.SingleOrDefault(i => i.ID == int.Parse(Context.RawUserID));
                    break;
                case UserIdentifierType.Username:
                    r = Resources.SingleOrDefault(i => i.Username == Context.RawUserID);
                    break;
            }

            return r;
        }

        internal override bool GetResourceAdminFlag()
        {
            bool isAdmin;
            int companyID = GetCompanyID();
            int resourceID = GetResourceID();
            string cacheKey = "";

            switch (Context.UserIDType)
            {
                case UserIdentifierType.ApiKey:
                    cacheKey = string.Format(CACHE_KEY_RESOURCE_ADMIN_APIKEY, companyID);
                    break;
                case UserIdentifierType.Email:
                    cacheKey = string.Format(CACHE_KEY_RESOURCE_ADMIN_EMAIL, companyID);
                    break;
                case UserIdentifierType.ID:
                    cacheKey = string.Format(CACHE_KEY_RESOURCE_ADMIN_ID, companyID);
                    break;
                case UserIdentifierType.Username:
                    cacheKey = string.Format(CACHE_KEY_RESOURCE_ADMIN_USERNAME, companyID);
                    break;
            }

            if (Caching.ListItemExists<bool, string>(cacheKey, Context.RawUserID))
            {
                isAdmin = Caching.GetItemInListByID<bool, string>(cacheKey, Context.RawUserID);
            }
            else
            {
                var r = Filter<CompanyResource>(i => i.CompanyID == companyID && i.ResourceID == resourceID).SingleOrDefault();
                if (r != null)
                {
                    isAdmin = r.IsAdministrator;
                    r = null;

                    Caching.SetItemInListByID<bool, string>(cacheKey, Context.RawUserID, isAdmin);
                }
                else
                {
                    isAdmin = false;
                }
            }

            return isAdmin;
        }

        public string createRandomPassword()
        {
            int MinimumPasswordLength = 7;
            //int MinimumNonAlphanumericLength = 0;
            
            string _allowedNonAlphaNumericChars = "!#$%";
            string _allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789" + _allowedNonAlphaNumericChars;
            Random randNum = new Random();
            char[] chars = new char[MinimumPasswordLength];

            for (int i = 0; i < MinimumPasswordLength; i++)
            {
                chars[i] = _allowedChars[(int)((_allowedChars.Length) * randNum.NextDouble())];
            }

            //for (int i = 0; i < MinimumNonAlphanumericLength; i++)
            //{
            //    chars[i] = _allowedChars[(int)((_allowedChars.Length) * randNum.NextDouble())];
            //}

            return new string(chars);
        }

        string hashPassword(string value)
        {
            SHA1 algorithm = SHA1.Create();
            byte[] data = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            string sh1 = "";
            for (int i = 0; i < data.Length; i++)
            {
                sh1 += data[i].ToString("x2").ToUpperInvariant();
            }
            return sh1;
        }

        public bool ChangePassword(int resourceID, string oldPassword, string newPassword)
        {
            var success = false;

            try
            {
                if (oldPassword != newPassword)
                {
                    var r = GetById<Resource>(resourceID); //Repo.GetById(resourceID);
                    if (r != null)
                    {
                        r.Password = hashPassword(newPassword);
                        Update<Resource>(r); //Repo.SaveOrUpdate(resource);
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

        public class AccessTokenResourceCacheModel
        {
            public AccessTokenResourceCacheModel()
            {
                Companies = new List<int>();
            }

            public string Username { get; set; }
            public string Token { get; set; }

            public List<int> Companies { get; set; }
        }

        public AccessTokenResourceCacheModel ValidateAccessTokenResource(string token)
        {
            string cacheKey = "AccessTokenResource";
            AccessTokenResourceCacheModel model = null;

            if (Caching.ListItemExists<AccessTokenResourceCacheModel, string>(cacheKey, token))
            {
                model = Caching.GetItemInListByID<AccessTokenResourceCacheModel, string>(cacheKey, token);
            }
            else
            {
                var resource = Filter<Resource>(i => i.ApiReadOnlyAccessToken == token, i => i.CompanyResources).SingleOrDefault();
                if (resource != null)
                {
                    model = new AccessTokenResourceCacheModel
                    {
                        Token = resource.APIPrivateKey,
                        Username = resource.Username,
                        Companies = resource.CompanyResources.Select(i => i.CompanyID).ToList()
                    };
                    Caching.SetItemInListByID<AccessTokenResourceCacheModel, string>(cacheKey, token, model);
                }
            }

            return model;
        }

        public class ApiResourceCacheModel
        {
            public ApiResourceCacheModel()
            {
                Companies = new List<int>();
            }

            public string Username { get; set; }
            public string Key { get; set; }
            public string Secret { get; set; }

            public List<int> Companies { get; set; }
        }

        public ApiResourceCacheModel ValidateApiResource(string key, string secret)
        {
            string cacheKey = "ApiResource";
            ApiResourceCacheModel model = null;

            if (Caching.ListItemExists<ApiResourceCacheModel, string>(cacheKey, key))
            {
                model = Caching.GetItemInListByID<ApiResourceCacheModel, string>(cacheKey, key);
                if (model.Secret != secret)
                {
                    model = null;
                }
            }
            else
            {
                var resource = Filter<Resource>(i => i.APIPublicKey == key, i => i.CompanyResources).SingleOrDefault();
                if (resource != null)
                {
                    if (resource.APIPrivateKey == secret)
                    {
                        model = new ApiResourceCacheModel
                        {
                            Key = resource.APIPublicKey,
                            Secret = resource.APIPrivateKey,
                            Username = resource.Username,
                            Companies = resource.CompanyResources.Select(i => i.CompanyID).ToList()
                        };
                        Caching.SetItemInListByID<ApiResourceCacheModel, string>(cacheKey, key, model);                    
                    }
                }
            }

            return model;
        }

        public Resource ValidateResource(string username, string password)
        {
            Resource r = null;

            switch(Context.UserIDType)
            {
                case UserIdentifierType.ApiKey:
                    #region
                    r = Filter<Resource>(i => i.APIPublicKey == username, i => i.ResourceType).SingleOrDefault();

                    if (r != null)
                    {
                        //int minSeconds = -10;
                        //int maxSeconds = 10;
                        //long secondsSinceEpoch = DateTime.UtcNow.Date.Epoch();
                        string secretKey = r.APIPrivateKey;

                        //var hash = new SHA256Managed();

                        bool isAuthorized = false;

                        //for (long i = (secondsSinceEpoch + minSeconds); i <= (secondsSinceEpoch + maxSeconds); i++)
                        //{
                        //string correctHash = secretKey + secondsSinceEpoch.ToString();
                        //byte[] unhashedBytes = Encoding.ASCII.GetBytes(correctHash); //encoding.GetBytes(correctHash);
                        //byte[] hashedBytes = hash.ComputeHash(unhashedBytes);
                        //correctHash = Convert.ToBase64String(hashedBytes);

                        //if (correctHash.Equals(password))
                        //{
                        //    isAuthorized = true;
                        //break;
                        //}
                        ///}

                        isAuthorized = secretKey.Equals(password);

                        if (!isAuthorized)
                        {
                            r = null;
                        }
                    }
                    break;
                    #endregion
                case UserIdentifierType.Email:
                case UserIdentifierType.Username:
                    #region
                    password = hashPassword(password);//System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(password, System.Web.Configuration.FormsAuthPasswordFormat.SHA1.ToString());
                    r = Filter<Resource>(i => i.Username == username && i.Password == password).SingleOrDefault();
                    break;
                    #endregion
                case UserIdentifierType.ID:
                    #region
                    break;
                    #endregion
            }

            // Check that resource has access to this company.
            if (r != null)
            {
                if (Filter<CompanyResource>(i => i.CompanyID == CurrentCompanyID && i.ResourceID == r.ID).Any())
                {
                    try
                    {
                        r.DateLastLoggedIn = DateTime.UtcNow;
                        Update<Resource>(r);
                    }
                    catch { }
                }
                else
                {
                    r = null;
                }
            }


            return r;
        }

        #endregion

        public string GetCompanySetting(string settingFieldName)
        {
            return CompanySettings.Single(i => i.CompanyID == CurrentCompanyID && i.Setting.FieldName == settingFieldName).Value;
        }

        public Dictionary<string, string> GetCompanySettings()
        {
            return Filter<CompanySetting>(i => i.CompanyID == CurrentCompanyID, i => i.Setting).ToDictionary(k => k.Setting.FieldName, v => v.Value);
        }

    }
}
