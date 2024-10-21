using d360.core.entities;
using d360.extensions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Linq.Expressions;

namespace d360.model
{
	[DbConfigurationType(typeof(AzureConfiguration))]
    public class CommunityContext : BaseContext, ICommunityContext
    {
        public CommunityContext(string connectionString, ISecurityContextProvider context)
            : base(connectionString)
        {
            Database.SetInitializer<CommunityContext>(null); // don't create any tables if they dont exist.
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
    }
}
