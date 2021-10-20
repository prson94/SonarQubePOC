using System.Linq;
using System.Collections.Generic;
using System.Data.Entity;
using d360.core.entities;
using System.Data.Entity.ModelConfiguration.Conventions;
using d360.extensions;
using System;
using System.Data.Entity.Infrastructure;
using d360.core.entities.Contracts;
using System.Linq.Expressions;
using System.Data;
using d360.core.exceptions;
using System.Data.SqlClient;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.SqlServer;
using System.Data.Common;
using Dapper;

namespace d360.model
{
    public class AzureConfiguration : DbConfiguration
    {
        public AzureConfiguration()
        {
            // The default retry limit is 5, which means that the total amount of time spent between retries is 26 seconds plus the random factor.
            SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy());
        }
    }

    public interface IDbContext
    {
        bool Add<T>(T item) where T : BaseObject;

        bool Delete<T>(T entity) where T : BaseObject;

        bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;

        int Execute(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null);

        bool Exists<T>(int id) where T : BaseIntObject;

        T GetById<T>(Guid id) where T : BaseGuidObject;

        T GetById<T>(Guid id, params Expression<Func<T, object>>[] includes) where T : BaseGuidObject;

        T GetById<T>(int id) where T : BaseIntObject;

        T GetById<T>(int id, params Expression<Func<T, object>>[] includes) where T : BaseIntObject;

        T GetByUid<T>(Guid uid, params Expression<Func<T, object>>[] includes) where T : BaseUidObject;

        IQueryable<T> GetWithIncludes<T>(params Expression<Func<T, object>>[] includes) where T : BaseObject;

        IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes) where T : BaseObject;

        IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, out int total, int index = 0, int size = 50, params Expression<Func<T, object>>[] includes) where T : BaseObject;

        IQueryable<T> Table<T>() where T : class;

        int SaveChanges();

        bool Update<T>(T item) where T : BaseObject;
    }

    [DbConfigurationType(typeof(AzureConfiguration))]
    public abstract class BaseContext : DbContext, IDisposable, IDbContext, IBaseContext
    {
        public int CurrentResourceID { get; set; }
        public int CurrentClientID { get; set; }
        public int CurrentCompanyID { get; set; }
        public int CurrentDomainSettingID { get; set; }
        public string CurrentCompanyDomain { get; set; }
        public bool CurrentResourceIsAdmin { get; set; }

        public string CompanyConnectionString { get; set; }
               
        internal ICachingProvider Caching;

        public ObjectContext ObjectContext
        {
            get
            {
                try
                {
                    return ((IObjectContextAdapter)this).ObjectContext;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public SqlConnection Connection { get { return (Database.Connection as SqlConnection); } }

        public BaseContext()
        {
            SetDefaultEntityFrameworkCommandTimeout();
        }

        public BaseContext(string connectionString): base(connectionString)
        {
            CompanyConnectionString = connectionString;
            SetDefaultEntityFrameworkCommandTimeout();
        }

        #region Generic Repository Methods

        public abstract bool Add<T>(T item) where T : BaseObject;

        public Exception CheckAndTranslateSqlException(SqlException ex, string objectName)
        {
            if (ex.Number == 2601)  //Primary key contraint error
            {
                return new DuplicateObjectException(objectName);
            }
            else
            {
                return ex;
            }
        }

        public abstract bool Delete<T>(Expression<Func<T, bool>> predicate) where T : BaseObject;

        public abstract bool Delete<T>(T entity) where T : BaseObject;

        public bool Exists<T>(int id) where T : BaseIntObject
        {
            return Set<T>().Any(i => i.ID == id);
        }

        public bool Any<T>(Expression<Func<T, bool>> expression) where T : BaseObject
        {
            return Set<T>().Any(expression);
        }

        public int Count<T>(Expression<Func<T, bool>> expression) where T : BaseObject
        {
            return Set<T>().Count(expression);
        }

        public IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes) where T : BaseObject
        {
            return GetWithIncludes<T>(includes).Where(predicate);
        }

        public IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, out int total, int index = 0, int size = 50, params Expression<Func<T, object>>[] includes) where T : BaseObject
        {
            int skipCount = index * size;
            var resetSet = predicate != null ?
                getWithIncludes<T>(includes).Where(predicate) :
                getWithIncludes<T>(includes).AsQueryable();
            resetSet = skipCount == 0 ?
                resetSet.Take<T>(size) :
                resetSet.Skip<T>(skipCount).Take<T>(size);
            total = resetSet.Count();
            return resetSet;
        }

        public IQueryable<T> GetWithIncludes<T>(params Expression<Func<T, object>>[] includes) where T : BaseObject
        {
            return getWithIncludes<T>(includes);
        }

        protected IEnumerable<BaseObject> GetChangedOrNewEntities()
        {
            const EntityState newOrModified = EntityState.Added | EntityState.Modified;
            return ObjectContext.ObjectStateManager.GetObjectStateEntries(newOrModified)
                .Where(x => x.Entity != null).Select(x => x.Entity as BaseObject);
        }

        public T GetById<T>(Guid id) where T : BaseGuidObject
        {
            return Set<T>().SingleOrDefault(i => i.ID == id) as T;
        }

        public T GetById<T>(Guid id, params Expression<Func<T, object>>[] includes) where T : BaseGuidObject
        {
            return getWithIncludes<T>(includes).Where(i => i.ID == id).SingleOrDefault();
        }


        public T GetById<T>(int id) where T : BaseIntObject
        {
            return Set<T>().SingleOrDefault(i => i.ID == id) as T;
        }

        public T GetById<T>(int id, params Expression<Func<T, object>>[] includes) where T : BaseIntObject
        {
            return getWithIncludes<T>(includes).Where(i => i.ID == id).SingleOrDefault();
        }

        public T GetById<T>(long id) where T : BaseLongObject
        {
            return Set<T>().SingleOrDefault(i => i.ID == id) as T;
        }

        public T GetById<T>(long id, params Expression<Func<T, object>>[] includes) where T : BaseLongObject
        {
            return getWithIncludes<T>(includes).Where(i => i.ID == id).SingleOrDefault();
        }

        public T GetByUid<T>(Guid uid, params Expression<Func<T, object>>[] includes) where T : BaseUidObject
        {
            return Set<T>().SingleOrDefault(i => i.Uid == uid) as T;
        }

        internal IQueryable<T> getWithIncludes<T>(params Expression<Func<T, object>>[] includes) where T : BaseObject
        {            
            var itemWithIncludes = Set<T>() as DbQuery<T>;
            if (includes.Length > 0)
            {
                foreach (var path in (includes[0].Body.ToString().Contains("Convert((") ? includes.Skip(1) : includes))
                {
                    itemWithIncludes = (DbQuery<T>)itemWithIncludes.Include(path); //ObjectQuery
                }
            }
            return itemWithIncludes.AsQueryable<T>();
        }

        public new IDbSet<T> Set<T>() where T : class
        {
            return base.Set<T>();
        }

        internal static bool IsPersistent(BaseObject entity)
        {
            if (entity is IIntObject)
            {
                return (entity as IIntObject).ID != 0;
            }
            else
            {
                return false;
            }
        }

        internal Exception resolveToRealException(Exception ex)
        {
            while (ex.Message.ToLower().Contains("inner exception for"))
            {
                ex = ex.InnerException;
            }
            return ex;
        }

        public int SaveOrUpdate<T>(T entity) where T : BaseIntObject
        {
            if (IsPersistent(entity))
            {
                Set<T>().Attach(entity);
                Entry(entity).State = EntityState.Modified;
            }
            else
            {
                this.Set<T>().Add(entity);
            }

            int numRecords = 0;

            try
            {
                numRecords = this.SaveChanges();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("committed successfully"))
                {
                    numRecords = 1;
                }
                else
                {
                    throw resolveToRealException(ex);
                }
            }
            catch (Exception ex)
            {
                throw resolveToRealException(ex);
            }

            return numRecords;
        }

        public abstract bool Update<T>(T item) where T : BaseObject;

        public DbDataReader Read(string sql)
        {
            var cmd = Database.Connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            if (Database.Connection.State != (ConnectionState.Open | ConnectionState.Fetching | ConnectionState.Executing))
            {
                Database.Connection.Open();
            }
            return cmd.ExecuteReader();
        }

        public IEnumerable<object[]> Read(DbDataReader reader)
        {
            while (reader.Read())
            {
                var values = new List<object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    values.Add(reader.GetValue(i));
                }
                yield return values.ToArray();
            }
        }

        public int Execute(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return ObjectContext.Connection.Execute(sql, param, transaction, commandTimeout, commandType);
        }

        public List<T> ExecuteQuery<T>(string commandText, List<SqlParameter> parameters)
        {
            return Database.SqlQuery<T>(commandText, parameters.ToArray()).ToList();
        }

        public void ExecuteNonQueryCommand(string commandText, List<SqlParameter> parameters)
        {
            var connection = new SqlConnection(Database.Connection.ConnectionString);
            try
            {
                var command = new SqlCommand();
                command.CommandTimeout = 1500;
                command.Connection = connection;

                connection.Open();

                command.CommandText = commandText;
                command.Parameters.AddRange(parameters.ToArray());
                command.ExecuteNonQuery();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    if (connection.State != ConnectionState.Closed)
                    {
                        connection.Close();
                    }
                }
            }
        }

        #endregion

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);

            base.Configuration.AutoDetectChangesEnabled = false;
            base.Configuration.ProxyCreationEnabled = false;
            base.Configuration.LazyLoadingEnabled = false;
        }

        public IQueryable<T> Table<T>() where T : class
        {
            return this.Set<T>();
        }

        private void SetDefaultEntityFrameworkCommandTimeout()
        {
            var adapter = (IObjectContextAdapter)this;
            if (adapter != null)
            {
                adapter.ObjectContext.CommandTimeout = 2 * 60; // 2 minute ef command timeout value in seconds (default is 30 seconds)
            }
        }

        #region For Deriving company and resource records based on incoming raw values

        #region Keys

        
        internal string CACHE_KEY_SSO_MODELS = "Company_SsoModels";
        internal string CACHE_KEY_CONNECTION_STRINGS = "Company_ConnectionStrings";

        #endregion

        #endregion
    }
}
