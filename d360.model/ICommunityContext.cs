using d360.core.entities;
using System;
using System.Data.Entity;
using System.Linq.Expressions;

namespace d360.model
{
	public interface ICommunityContext : IBaseContext
    {
        DbSet<CompanyResource> CompanyResources { get; set; }
        
        DbSet<Resource> Resources { get; set; }        

        new bool Add<T>(T item) where T : BaseObject;
        
        new bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        
        new bool Delete<T>(T entity) where T : BaseObject;
        
        int SaveChanges();
        
        new bool Update<T>(T item) where T : BaseObject;
    }
}