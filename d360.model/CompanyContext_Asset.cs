using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
    public class AssetResults
    {
        public int Count { get; set; }
        public IEnumerable<dynamic> Results { get; set; }
    }

    partial class CompanyContext : BaseContext
    {
        #region DbSets

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetDetail> AssetDetails { get; set; }

        public DbSet<AssetType> AssetTypes { get; set; }

        public DbSet<AssetApiModel> AssetApiModels { get; set; }

        public DbSet<FieldApiModel> FieldApiModels { get; set; }

        #endregion

        #region Engine Methods

        public string GetEscapedFilterString(string filter, bool isContains = false)
        {
            return wildcardValue(escapeForSQLLike(filter), isContains);
        }

        private string wildcardValue(string value, bool isContains = true)
        {
            value = value.Replace("*", "%").Replace("?", "_");
            value = isContains ? $"%{value}%" : $"{value}%";
            return value;
        }

        private string escapeForSQLLike(string value, bool isContains = true)
        {
            char[] escapeChars = new char[] { '%', '_', '^', '[' };
            string escapedValue = "";

            foreach (char c in value)
            {
                if (escapeChars.Contains(c))
                {
                    escapedValue += $"[{c}]";
                }
                else
                {
                    escapedValue += c;
                }
            }
            return escapedValue;
        }

        public string DetermineSqlDataTypeForFieldType(FieldType f)
        {
            var sqlDataType = "nvarchar";
            if (f.Type == DataType.JsonElement.ToString())
            {
                FieldTypeDefinition_JsonElement jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(f.Definition);
                sqlDataType = jsonElementDefinition.DataType;
                jsonElementDefinition = null;
            }
            else if (f.Type == DataType.Boolean.ToString())
            {
                sqlDataType = "bit";
            }
            else if (f.Type == DataType.Date.ToString())
            {
                sqlDataType = "date";
            }
            else if (f.Type == DataType.DateTime.ToString())
            {
                sqlDataType = "datetime";
            }
            else if (f.Type == DataType.Decimal.ToString())
            {
                sqlDataType = "decimal";
            }
            else if (f.Type == DataType.Number.ToString())
            {
                sqlDataType = "int";
            }

            return sqlDataType;
        }

        public void SynchronizeExecutionAssetsWithGraph(Guid executionUid)
        {
            Connection.Execute("exec[graph].[SynchronizeAssetExecution] @executionUid", new { executionUid });
        }

        public void SynchronizeExecutionRelationshipWithGraph(Guid executionUid)
        {
            Connection.Execute("[graph].[SynchronizeRelationshipExecution] @executionUid", new { executionUid });
        }

        public void UpdateAssetNode(Guid assetUid)
        {
            Connection.Execute("[graph].[UpdateAssetNode] @assetuid, 1", new { assetUid });
        }

        #endregion
    }
}
