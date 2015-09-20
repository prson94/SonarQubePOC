using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities;
using System.Linq.Expressions;

namespace d360.model.interfaces
{
    public interface IRepository<T, TIdentifier> where T : BaseObject
    {
        int CurrentResourceID { get; }
        bool Contains(Expression<Func<T, bool>> predicate);
        int Count();
        bool Delete(Expression<Func<T, bool>> predicate);
        bool Delete(T entity);
        IQueryable<T> Filter(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        IQueryable<T> Filter<Key>(Expression<Func<T, bool>> predicate, out int total, int index = 0, int size = 50, params Expression<Func<T, object>>[] includes);
        IQueryable<T> GetAll(params Expression<Func<T, object>>[] includes);
        T GetById(TIdentifier id, params Expression<Func<T, object>>[] includes);
        int SaveOrUpdate(T entity);

    } 
}
