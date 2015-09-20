using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.model;

namespace d360.extensions.info
{
    public class SecurityContextLookupProvider : ISecurityContextLookupProvider
    {
        public string ConnectionString 
        { 
            get 
            {
                var c = GetCompany();
                return string.Format(
                    "server={0};Database=D3S_{1};User ID={2};Password={3}", 
                    c.DatabaseServer.Server, 
                    c.ID, 
                    c.DatabaseServer.Username, 
                    c.DatabaseServer.Password
                );
            }
        }

        #region DI

        CommunityContext Community;
        ICachingProvider CacheProvider;
        ISecurityContextProvider ContextProvider;

        public SecurityContextLookupProvider(CommunityContext community, ICachingProvider cacheProvider, ISecurityContextProvider contextProvider)
        {
            Community = community;
            CacheProvider = cacheProvider;
            ContextProvider = contextProvider;
        }

        #endregion

        #region Keys

        string CACHE_KEY_COMPANY_ID = "CompanyID_ID";
        string CACHE_KEY_COMPANY_PUBLICID = "CompanyID_PublicID";
        string CACHE_KEY_COMPANY_URI = "CompanyID_Uri";

        string CACHE_KEY_RESOURCE_APIKEY = "ResourceID_ApiKey";
        string CACHE_KEY_RESOURCE_EMAIL = "ResourceID_Email";
        string CACHE_KEY_RESOURCE_ID = "ResourceID_ID";
        string CACHE_KEY_RESOURCE_USERNAME = "ResourceID_Username";

        string CACHE_KEY_RESOURCE_ADMIN_APIKEY = "Resource_{0}_Admin_ApiKey";
        string CACHE_KEY_RESOURCE_ADMIN_EMAIL = "Resource_{0}_Admin_Email";
        string CACHE_KEY_RESOURCE_ADMIN_ID = "Resource_{0}_Admin_ID";
        string CACHE_KEY_RESOURCE_ADMIN_USERNAME = "Resource_{0}_Admin_Username";

        #endregion

        public Company GetCompany()
        {
            CurrentCompanyInfo info = ContextProvider.GetCurrentCompanyInfo();
            Company c = null;

            switch (info.Type)
            {
                case CompanyIdentifierType.ID:
                    int iID;
                    if (int.TryParse(info.Identifier, out iID))
                    {
                        c = Community.GetById<Company>(iID, i => i.DatabaseServer);
                    }
                    break;
                case CompanyIdentifierType.PublicID:
                    Guid gID;
                    if (Guid.TryParse(info.Identifier, out gID))
                    {
                        c = Community.Filter<Company>(i => i.PublicID == gID, i => i.DatabaseServer).SingleOrDefault();
                    }
                    break;
                case CompanyIdentifierType.Uri:
                    c = Community.Filter<Company>(i => i.UrlPrefix == info.Identifier, i => i.DatabaseServer).SingleOrDefault();
                    break;
            }

            return c;
        }

        public int GetCompanyID()
        {
            CurrentCompanyInfo info = ContextProvider.GetCurrentCompanyInfo();
            int id = 0;
            string cacheKey = "";

            switch (info.Type)
            {
                case CompanyIdentifierType.ID:
                    cacheKey = CACHE_KEY_COMPANY_ID;
                    break;
                case CompanyIdentifierType.PublicID:
                    cacheKey = CACHE_KEY_COMPANY_PUBLICID;
                    break;
                case CompanyIdentifierType.Uri:
                    cacheKey = CACHE_KEY_COMPANY_URI;
                    break;
            }

            if (CacheProvider.ListItemExists<int, string>(cacheKey, info.Identifier))
            {
                id = CacheProvider.GetItemInListByID<int, string>(cacheKey, info.Identifier);
            }
            else
            {
                var c = GetCompany();
                if (c != null) id = c.ID;
                c = null;
                CacheProvider.SetItemInListByID<int, string>(cacheKey, info.Identifier, id);
            }

            return id;
        }

        public Resource GetResource()
        {
            CurrentUserInfo info = ContextProvider.GetCurrentUserInfo();
            Resource r = null;

            switch (info.Type)
            {
                case UserIdentifierType.ApiKey:
                    r = Community.Resources.SingleOrDefault(i => i.APIPublicKey == info.Identifier);
                    break;
                case UserIdentifierType.Email:
                    r = Community.Resources.SingleOrDefault(i => i.Email == info.Identifier);
                    break;
                case UserIdentifierType.ID:
                    r = Community.Resources.SingleOrDefault(i => i.ID == int.Parse(info.Identifier));
                    break;
                case UserIdentifierType.Username:
                    r = Community.Resources.SingleOrDefault(i => i.Username == info.Identifier);
                    break;
            }

            return r;
        }

        public int GetResourceID()
        {
            CurrentUserInfo info = ContextProvider.GetCurrentUserInfo();
            int id;
            string cacheKey = "";

            switch (info.Type)
            {
                case UserIdentifierType.ApiKey:
                    cacheKey = CACHE_KEY_RESOURCE_APIKEY;
                    break;
                case UserIdentifierType.Email:
                    cacheKey = CACHE_KEY_RESOURCE_EMAIL;
                    break;
                case UserIdentifierType.ID:
                    cacheKey = CACHE_KEY_RESOURCE_ID;
                    break;
                case UserIdentifierType.Username:
                    cacheKey = CACHE_KEY_RESOURCE_USERNAME;
                    break;
            }

            if (CacheProvider.ListItemExists<int, string>(cacheKey, info.Identifier))
            {
                id = CacheProvider.GetItemInListByID<int, string>(cacheKey, info.Identifier);
            }
            else
            {
                var r = GetResource();
                id = 0;
                if (r != null)
                {
                    id = r.ID;
                    r = null;
                }
                CacheProvider.SetItemInListByID<int, string>(cacheKey, info.Identifier, id);
            }

            return id;
        }

        public bool GetResourceAdminFlag()
        {
            CurrentUserInfo info = ContextProvider.GetCurrentUserInfo();
            bool isAdmin;
            int companyID = GetCompanyID();
            int resourceID = GetResourceID();
            string cacheKey = "";

            switch (info.Type)
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

            if (CacheProvider.ListItemExists<bool, string>(cacheKey, info.Identifier))
            {
                isAdmin = CacheProvider.GetItemInListByID<bool, string>(cacheKey, info.Identifier);
            }
            else
            {
                var r = Community.CompanyResources.SingleOrDefault(i => i.CompanyID == companyID && i.ResourceID == resourceID);
                if (r != null)
                {
                    isAdmin = r.IsAdministrator;
                    r = null;

                    CacheProvider.SetItemInListByID<bool, string>(cacheKey, info.Identifier, isAdmin);
                }
                else
                {
                    isAdmin = false;
                }
            }

            return isAdmin;
        }
    }
}
