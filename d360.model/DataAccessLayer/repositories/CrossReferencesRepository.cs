using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class CrossReferencesRepository : ICrossReferencesRepository
    {
        ICompanyContext CompanyContext;
        public CrossReferencesRepository(ICompanyContext compCtx)
        {
            this.CompanyContext = compCtx;
        }
        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferences(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            var sql = "select uid, DataSource, Type, ExternalID, FieldHash from [dbo].[AssetCrossReference]";
            List<string> queryFilters = new List<string>();


            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_assetuid"))
            {
                Guid assetUid = new Guid();

                var assetUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_assetuid").Value;
                if (Guid.TryParse(assetUidString, out assetUid))
                {
                    dbArgs.Add("@assetuid", assetUid);
                    queryFilters.Add($"[UID] = @assetuid");
                }
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_externalid"))
            {
                var externalId = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_externalid").Value;
                dbArgs.Add("@externalid", externalId);
                queryFilters.Add($"[ExternalID] = @externalid");
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_datasource"))
            {
                var ds = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_datasource").Value;
                dbArgs.Add("@datasource", ds);
                queryFilters.Add($"[DataSource] = @datasource");
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_type"))
            {
                var ty = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_type").Value;
                dbArgs.Add("@type", ty);
                queryFilters.Add($"[type] = @type");
            }

            if (queryFilters.Count > 0)
            {
                sql += " where " + string.Join(" and ", queryFilters);
            }
            var assetCrossReferences = await CompanyContext.QueryAsync<AssetCrossReference>(sql, dbArgs);
            return assetCrossReferences;
        }

        public async Task<IEnumerable<AssetCrossReference>> GetByAssetUid(string assetUid)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where uid = @assetUid", new { assetUid });
        }

        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByTypeId(string type, string externalId)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type and [ExternalID] = @externalId", new { type = new DbString { Value = type, IsFixedLength = true, Length = 50, IsAnsi = true }, externalId });
        }


        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByType(string type)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type", new { type = new DbString { Value = type, IsFixedLength = true, Length = 50, IsAnsi = true } }); 
        }

        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByDataSource(string dataSource)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [datasource] = @dataSource", new { dataSource = new DbString { Value = dataSource, IsFixedLength = true, Length = 250, IsAnsi = true } });
        }


        public async Task<int> CreateNewCrossReference(AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("insert into assetcrossreference (uid,DataSource,Type,ExternalID,FieldHash) values(@u,@d,@t,@e,@f)", new { u = model.uid, d = model.DataSource, t = model.Type, f = model.FieldHash, e = model.ExternalID });
        }


        public async Task<bool> PostBulkCrossReference(List<AssetCrossReference> models)
        {
            if (CompanyContext.Database.Connection.State != ConnectionState.Open)
                CompanyContext.Database.Connection.Open();
            // bcp the records in
            using (var bulkCopy = new SqlBulkCopy((CompanyContext.Database.Connection) as SqlConnection))
            {
                bulkCopy.BatchSize = models.Count;
                bulkCopy.DestinationTableName = "AssetCrossReference";
                bulkCopy.BulkCopyTimeout = 300;

                var table = new DataTable();
                var columnName = "uid";
                table.Columns.Add(columnName, typeof(Guid));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "DataSource";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "Type";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "ExternalID";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "FieldHash";
                table.Columns.Add(columnName, typeof(string));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in models)
                {
                    var row = table.NewRow();

                    row["uid"] = item.uid;
                    row["DataSource"] = item.DataSource;
                    row["Type"] = item.Type;
                    row["ExternalID"] = item.ExternalID;
                    row["FieldHash"] = item.FieldHash;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            return true;
        }

        public async Task<int> PutCrossReference(Guid uid, string dataSource, string type, AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("update assetcrossreference set FieldHash = @fh where uid = @uid and DataSource = @ds and [Type] = @t", new { fh = model.FieldHash, uid = uid, ds = dataSource, t = type });
        }

        public async Task<int> PutCrossReference(Guid uid, AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("update assetcrossreference set FieldHash = @fh where uid = @uid and DataSource = @ds and [Type] = @t", new { fh = model.FieldHash, uid = uid, ds = model.DataSource, t = model.Type });
        }


        public async Task<int> DeleteCrossReferenceByUid(Guid uid)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where uid = @uid", new { uid = uid });
        }

        public async Task<int> DeleteCrossReferenceByDataSource(string dataSource, string type)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where datasource = @d and [type] = @t", new { d = dataSource, t = type });
        }

        public async Task<int> DeleteCrossReferenceByDataSource(string dataSource)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where [datasource] = @d", new { d = dataSource });
        }

        public async Task<int> DeleteCrossReferenceByType(string type)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where [type] = @t", new { t = type });
        }



        public async Task<bool> XrefExists(AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.QuerySingleAsync<bool>(@"if exists (select 1 from assetcrossreference where uid = @u and [type] = @t and datasource = @d and externalid = @e)
                        begin
                            select 1;
                                end
                        else 
                        begin
                            select 0;
                                end", new { u = model.uid, t = model.Type, d = model.DataSource, e = model.ExternalID });

        }

    }
}
