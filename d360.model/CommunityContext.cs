using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.extensions;

using Dapper;

namespace d360.model
{
    [DbConfigurationType(typeof(AzureConfiguration))]
    public class CommunityContext : BaseContext, ICommunityContext
    {
        public CommunityContext(string connectionString, ICachingProvider caching, ISecurityContextProvider context)
            : base(connectionString)
        {
            Database.SetInitializer<CommunityContext>(null); //dont create any tables if they dont exist.
            Caching = caching;
        }

        public DbSet<CompanyResource> CompanyResources { get; set; }
        
        public DbSet<Resource> Resources { get; set; }

		public DbSet<CompanyDigestExecution> CompanyDigestExecution { get; set; }

		public override bool Add<T>(T item)
        {
            Set<T>().Add(item);
            return SaveChanges() > 0;
        }

        public override bool Delete<T>(Expression<Func<T, bool>> predicate)
        {
            List<T> items = Filter(predicate).ToList();
            bool allDeleted = true;

            items.ForEach(i =>
            {
                if (!Delete(i))
                {
                    allDeleted = false;
                }
            });

            SaveChanges();

            return allDeleted;
        }

        public override bool Delete<T>(T entity)
        {
            Set<T>().Remove(entity);
            SaveChanges();
            return true;
        }

        public override int SaveChanges()
        {
            int returnValue = 0;

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
            ChangeTracker.DetectChanges();

            return SaveChanges() > 0;
        }

        public string GetCompanyConnectionString(int companyId, bool skipCacheCheck = false)
        {
            string cs;

            if (Caching.ListItemExists<string, int>(CACHE_KEY_CONNECTION_STRINGS, companyId) && !skipCacheCheck)
            {
                cs = Caching.GetItemInListByID<string, int>(CACHE_KEY_CONNECTION_STRINGS, companyId);
				if (cs != null)
				{
					return cs;
				}
            }
            
            dynamic res = Database.Connection.QuerySingle(@"select s.Server, s.Username, s.Password from Company c
                            inner join DatabaseServer s on s.ID = c.DatabaseServerID 
                            where c.ID = @companyId", new { companyId = companyId });

            cs = CompanyConnectionStringHelper.ConnectionString(companyId, res.Server, res.Username, res.Password);

            if (!skipCacheCheck)
            {
                Caching.SetItemInListByID(CACHE_KEY_CONNECTION_STRINGS, companyId, cs);
            }

            return cs;
        }
    }
}
