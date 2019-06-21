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

        private string wildcardValue(string value)
        {
            if (value.Contains("*") || value.Contains("?"))
                return value.Replace("*", "%").Replace("?", "_") + "%";
            else
                return value += "%";
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

        /// <summary>
        /// This is the stored procedure version of getting both the count and paged assets with relevant dynamic fields.
        /// </summary>
        public async Task<AssetResults> GetDynamicAssets(AssetType at, List<UiRequestFilterValue> filters, int pageNumber = 0, int pageSize = 25, string sortField = "", string sortOrder = "", string simpleFilter = null, bool apiNamesInOutput = false, bool listableFieldsOnly = true, bool pagingEnabled = true)
        {
            var results = new AssetResults
            {
                Count = 0
            };

            #region Process Filters

            var parameters = new DynamicParameters();
            var filterTable = new System.Data.DataTable();
            filterTable.SetTypeName("AssetFiltersTable");
            filterTable.Columns.Add(new System.Data.DataColumn("FilterType"));
            filterTable.Columns.Add(new System.Data.DataColumn("Operator"));
            filterTable.Columns.Add(new System.Data.DataColumn("TypeID"));
            filterTable.Columns.Add(new System.Data.DataColumn("OptionalIdentifier"));
            filterTable.Columns.Add(new System.Data.DataColumn("FilterValues"));

            if (string.IsNullOrEmpty(simpleFilter))
            {
                foreach (var filter in filters)
                {
                    if (filter is UiRequestAttributeFilterValue)
                    {
                        var f = filter as UiRequestAttributeFilterValue;
                        filterTable.Rows.Add("A", f.Operator, f.AttributeTypeID, null, $"{wildcardValue(f.RawValue)}");
                    }

                    if (filter is UiRequestFieldFilterValue)
                    {
                        var f = filter as UiRequestFieldFilterValue;

                        if (f.IsParentField)
                        {
                            filterTable.Rows.Add("P", f.Operator, 0, null, $"{wildcardValue(f.RawValue)}");
                        }
                        else
                        {
                            filterTable.Rows.Add("F", f.Operator, int.Parse(f.FieldName.Replace("Field", "")), null, $"{wildcardValue(f.RawValue)}");
                        }
                    }

                    if (filter is UiRequestOwnershipFilterValue)
                    {
                        var f = filter as UiRequestOwnershipFilterValue;
                        var arr = new JArray(f.Items.Select(o => o.GetAsJsonDbQueryObject()));
                        filterTable.Rows.Add("O", f.Operator, 0, null, arr.ToString(Formatting.None));
                    }

                    if (filter is UiRequestRelationshipFilterValue)
                    {
                        var f = filter as UiRequestRelationshipFilterValue;
                        var idList = string.Join(",", f.TargetObjectIDs);
                        filterTable.Rows.Add("R", f.Operator, f.IntersectTypeID, f.TargetObject, idList);
                    }

                    if (filter is UiRequestRelationshipFieldFilterValue)
                    {
                        var f = filter as UiRequestRelationshipFieldFilterValue;
                        filterTable.Rows.Add("F", f.Operator, f.FieldTypeID, null, $"{wildcardValue(f.RawValue)}");
                    }
                }
            }
            else
            {
                simpleFilter = wildcardValue(simpleFilter);
            }

            #endregion

            // Scrub sort fields
            sortField = sortField.Replace("'", "").Replace(" ", "").Replace("-","");
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "" : (sortOrder.ToLower().Equals("asc") ? "asc" : "desc");

            parameters.Add("assetTypeId", at.ID);
            parameters.Add("userId", CurrentResourceID);
            parameters.Add("filter", simpleFilter);
            parameters.Add("pageNumber", pageNumber);
            parameters.Add("pageSize", pageSize);
            parameters.Add("sortField", sortField);
            parameters.Add("sortDirection", sortOrder);
            parameters.Add("filters", filterTable);
            parameters.Add("apiNamesInOutput", apiNamesInOutput);
            parameters.Add("listableFieldsOnly", listableFieldsOnly);
            parameters.Add("pagingEnabled", pagingEnabled);

            var multi = await QueryMultipleAsync(
                "exec GetDynamicAssets @assetTypeId, @userId, @filter, @apiNamesInOutput, @listableFieldsOnly, @pagingEnabled, @pageNumber, @pageSize, @sortField, @sortDirection, @filters",
                parameters);
            results.Count = multi.Read<int>().First();
            results.Results = multi.Read<dynamic>().ToList();

            return results;
        }

        #endregion
    }
}
