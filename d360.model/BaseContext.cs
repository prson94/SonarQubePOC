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
            SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy(3, TimeSpan.FromSeconds(5)));
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

        IQueryable<T> GetWithIncludes<T>(params Expression<Func<T, object>>[] includes) where T : BaseObject;

        IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes) where T : BaseObject;

        IQueryable<T> Filter<T>(Expression<Func<T, bool>> predicate, out int total, int index = 0, int size = 50, params Expression<Func<T, object>>[] includes) where T : BaseObject;

        IQueryable<T> Table<T>() where T : class;

        int SaveChanges();

        bool Update<T>(T item) where T : BaseObject;
    }

    [DbConfigurationType(typeof(AzureConfiguration))]
    public abstract class BaseContext : DbContext, IDisposable, IDbContext
    {
        public int CurrentResourceID { get; set; }
        public int CurrentCompanyID { get; set; }
        public string CurrentCompanyDomain { get; set; }
        public bool CurrentResourceIsAdmin { get; set; }

        public string CompanyConnectionString { get; set; }

        internal ISecurityContextProvider Context;
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

        public BaseContext()
        {

        }

        public BaseContext(string connectionString): base(connectionString)
        {
            CompanyConnectionString = connectionString;
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

        internal IQueryable<T> getWithIncludes<T>(params Expression<Func<T, object>>[] includes) where T : BaseObject
        {
            //ObjectQuery<T> itemWithIncludes = Set<T>() as ObjectQuery<T>;
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
                this.Set<T>().Add(entity);

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
                Database.Connection.Open();
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
                        connection.Close();
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

        public void UpdateDatabaseTableStatistics()
        {
            ExecuteNonQueryCommand("sp_updatestats", null);
        }

        #region For Deriving company and resource records based on incoming raw values

        #region Keys

        //string CACHE_KEY_COMPANY_ID = "CompanyID_ID";
        //string CACHE_KEY_COMPANY_PUBLICID = "CompanyID_PublicID";
        //string CACHE_KEY_COMPANY_URI = "CompanyID_Uri";

        //string CACHE_KEY_RESOURCE_APIKEY = "ResourceID_ApiKey";
        //string CACHE_KEY_RESOURCE_EMAIL = "ResourceID_Email";
        //string CACHE_KEY_RESOURCE_ID = "ResourceID_ID";
        //string CACHE_KEY_RESOURCE_USERNAME = "ResourceID_Username";
        //string CACHE_KEY_RESOURCE_ACCESSTOKEN = "ResourceID_AccessToken";

        internal string CACHE_KEY_SSO_MODELS = "Company_SsoModels";
        internal string CACHE_KEY_CONNECTION_STRINGS = "Company_ConnectionStrings";
        internal string CACHE_KEY_RESOURCE_ADMIN_APIKEY = "Resource_{0}_Admin_ApiKey";
        internal string CACHE_KEY_RESOURCE_ADMIN_EMAIL = "Resource_{0}_Admin_Email";
        internal string CACHE_KEY_RESOURCE_ADMIN_ID = "Resource_{0}_Admin_ID";
        internal string CACHE_KEY_RESOURCE_ADMIN_USERNAME = "Resource_{0}_Admin_Username";

        #endregion

        //internal abstract Company GetCompany();

        //internal abstract Resource GetResource();

        //internal abstract bool GetResourceAdminFlag();

        //internal int GetCompanyID()
        //{
        //    int id = 0;
        //    string cacheKey = "";

        //    switch (Context.CompanyIDType)
        //    {
        //        case CompanyIdentifierType.ID:
        //            cacheKey = CACHE_KEY_COMPANY_ID;
        //            break;
        //        case CompanyIdentifierType.PublicID:
        //            cacheKey = CACHE_KEY_COMPANY_PUBLICID;
        //            break;
        //        case CompanyIdentifierType.Uri:
        //            cacheKey = CACHE_KEY_COMPANY_URI;
        //            break;
        //    }

        //    if (Caching.ListItemExists<int, string>(cacheKey, Context.RawCompanyID))
        //    {
        //        id = Caching.GetItemInListByID<int, string>(cacheKey, Context.RawCompanyID);
        //    }
        //    else
        //    {
        //        var c = GetCompany();
        //        if (c != null) id = c.ID;
        //        c = null;
        //        Caching.SetItemInListByID<int, string>(cacheKey, Context.RawCompanyID, id, true, 5);
        //    }

        //    return id;
        //}

        //internal int GetResourceID()
        //{
        //    int id;
        //    string cacheKey = "";

        //    switch (Context.UserIDType)
        //    {
        //        case UserIdentifierType.ApiKey:
        //            cacheKey = CACHE_KEY_RESOURCE_APIKEY;
        //            break;
        //        case UserIdentifierType.Email:
        //            cacheKey = CACHE_KEY_RESOURCE_EMAIL;
        //            break;
        //        case UserIdentifierType.ID:
        //            cacheKey = CACHE_KEY_RESOURCE_ID;
        //            break;
        //        case UserIdentifierType.Username:
        //            cacheKey = CACHE_KEY_RESOURCE_USERNAME;
        //            break;
        //        case UserIdentifierType.AccessToken:
        //            cacheKey = CACHE_KEY_RESOURCE_ACCESSTOKEN;
        //            break;
        //    }

        //    if (Caching.ListItemExists<int, string>(cacheKey, Context.RawUserID))
        //    {
        //        id = Caching.GetItemInListByID<int, string>(cacheKey, Context.RawUserID);
        //    }
        //    else
        //    {
        //        var r = GetResource();
        //        id = 0;
        //        if (r != null)
        //        {
        //            id = r.ID;
        //            r = null;
        //        }
        //        Caching.SetItemInListByID<int, string>(cacheKey, Context.RawUserID, id);
        //    }

        //    return id;
        //}

        #endregion
    }
}
