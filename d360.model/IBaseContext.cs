using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using d360.core.entities;

namespace d360.model
{
    public interface IBaseContext
    {
        string CompanyConnectionString { get; set; }
        SqlConnection Connection { get; }
        string CurrentCompanyDomain { get; set; }
        int CurrentClientID { get; set; }
        int CurrentCompanyID { get; set; }
        int CurrentDomainSettingID { get; set; }
        int CurrentResourceID { get; set; }
        bool CurrentResourceIsAdmin { get; set; }
        ObjectContext ObjectContext { get; }

        bool Add<T>(T item) where T : BaseObject;
        bool Any<T>(Expression<Func<T, bool>> expression) where T : BaseObject;
        Exception CheckAndTranslateSqlException(SqlException ex, string objectName);
        int Count<T>(Expression<Func<T, bool>> expression) where T : BaseObject;
        bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;
        bool Delete<T>(T entity) where T : BaseObject;
        int Execute(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null);
        void ExecuteNonQueryCommand(string commandText, List<SqlParameter> parameters);
        List<T> ExecuteQuery<T>(string commandText, List<SqlParameter> parameters);
        bool Exists<T>(int id) where T : BaseIntObject;
        IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, out int total, int index = 0, int size = 50, params Expression<Func<T, object>>[] includes) where T : BaseObject;
        IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes) where T : BaseObject;
        T GetById<T>(Guid id) where T : BaseGuidObject;
        T GetById<T>(Guid id, params Expression<Func<T, object>>[] includes) where T : BaseGuidObject;
        T GetById<T>(int id) where T : BaseIntObject;
        T GetById<T>(int id, params Expression<Func<T, object>>[] includes) where T : BaseIntObject;
        T GetById<T>(long id) where T : BaseLongObject;
        T GetById<T>(long id, params Expression<Func<T, object>>[] includes) where T : BaseLongObject;
        T GetByUid<T>(Guid uid, params Expression<Func<T, object>>[] includes) where T : BaseUidObject;
        IQueryable<T> GetWithIncludes<T>(params Expression<Func<T, object>>[] includes) where T : BaseObject;
        IEnumerable<object[]> Read(DbDataReader reader);
        DbDataReader Read(string sql);
        int SaveOrUpdate<T>(T entity) where T : BaseIntObject;
        IDbSet<T> Set<T>() where T : class;
        IQueryable<T> Table<T>() where T : class;
        bool Update<T>(T item) where T : BaseObject;        
    }
}