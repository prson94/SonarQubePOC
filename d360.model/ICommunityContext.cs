using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq.Expressions;
using System.Threading.Tasks;

using d360.core.entities;

namespace d360.model
{
    public interface ICommunityContext : IBaseContext
    {
        DbSet<CompanyResource> CompanyResources { get; set; }
        
        DbSet<Resource> Resources { get; set; }        

        new bool Add<T>(T item) where T : BaseObject;
        
        new bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        
        new bool Delete<T>(T entity) where T : BaseObject;
        
        string GetCompanyConnectionString(int companyId, bool skipCacheCheck = false);
        
        int SaveChanges();
        
        new bool Update<T>(T item) where T : BaseObject;
    }
}