using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using d360.core;
using d360.core.entities;
using d360.model.helpers.filters.program;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d360.model.helpers.filters
{
	public class TempTablePathSegmentToken : FilterBaseToken, IFilterToken, ITempTableFilter, ISegmentPathFilterToken
	{
		private IFieldValueValidator fieldValueValidator;
		private AdvancedFilterTempTableInfo tempTableInfo = new AdvancedFilterTempTableInfo();
		private List<AssetTypeKeyFieldMap> AssetTypeKeyFieldMaps = new List<AssetTypeKeyFieldMap>();

		public TempTablePathSegmentToken(IFilterDataProvider fdp, string field, string op, object value, List<AssetTypeKeyFieldMap> typeKeyFieldMap, int? paramIdx = null)
		{
			dataProvider = fdp;
			parameterIdx = paramIdx ?? -1;
			this.field = field;
			@operator = op;
			this.value = value;
			this.AssetTypeKeyFieldMaps = typeKeyFieldMap;

			if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
			{
				IsNullValue = true;
			}
		}

		public string GetSqlExpression(Dictionary<string, object> sqlParams)
		{
			if (field == null)
			{
				throw new MethodAccessException("Method can be used only when Field Type is loaded. Use LoadFieldType() method before.");
			}

			sqlParamsRef = sqlParams;
			stringBuilder.Clear();

			if (!IsNullValue)
			{
				NotNullValueExpression();
			}
			else
			{
				UpdateTokenForNullValue();
			}

			return stringBuilder.ToString();
		}

		void UpdateTokenForNullValue()
		{
			if (!new[] { "eq", "ne" }.Contains(@operator))
			{
				throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
			}

			var fieldSql = GetColumnValueSyntax(fieldType.ID);

			stringBuilder.Append(fieldSql);
			stringBuilder.Append(FilterHelpers.GetSQLNullOperator(@operator));
		}



		void NotNullValueExpression()
		{
			if (!FilterHelpers.IsValidOperatorForFieldType(CurrentFieldType, @operator))
			{
				throw new FilterExpressionParserException($"Operator '{@operator}' is not valid for '{CurrentFieldType}' on field {field}");
			}

			FilterHelpers.ValidateValueForType(CurrentFieldType, value);

			var valueValidation = fieldValueValidator.CheckValue(value, field, @operator);

			if (!valueValidation.Status)
			{
				throw new FormatException(valueValidation.Message);
			}

			value = valueValidation.UpdatedValue;
			UpdateValueWithWildCards();

			var fieldSql = GetColumnValueSyntax(fieldType.ID);

			stringBuilder.Append($"(A.ID in (select AssetID from #advanced_filter_{parameterIdx}))");

			string filterExpression = $"{fieldSql} {FilterHelpers.GetSQLOperator(@operator)} @filter_{parameterIdx}";
			var definition = JsonConvert.DeserializeObject<JObject>(fieldType.Definition ?? "{}");

			Guid uid = Guid.Parse(definition.GetValue("AssetTypeUid").ToString());

			string matchField = $"[lvl_{uid}]";
			var keyFields = AssetTypeKeyFieldMaps.Where(x => x.AssetTypeUid == uid);

			this.tempTableInfo.TempTableQuery = "";
			string filteredTable = $"#filtered_hierarchy_{parameterIdx}";

			if (keyFields.Count() == 0 || keyFields.Count() > 1)
			{
				this.tempTableInfo.TempTableQuery = @$"
									drop table if exists {filteredTable}
									create table {filteredTable} (AssetId int)

									insert into {filteredTable} (AssetId)
									select a.ID from AssetType at
									inner join Asset a on a.AssetTypeID = at.ID
									cross apply dbo.GetAssetDisplayValueById(a.id)Val
									where at.uid = '{uid}' and val.DisplayValue like @filter_{parameterIdx}
									option(recompile)";
			}
			else
			{
				this.tempTableInfo.TempTableQuery = @$"
									drop table if exists {filteredTable}
									create table {filteredTable} (AssetId int)

									insert into #filtered_hierarchy_{parameterIdx} (AssetId)
									select AssetId from Field f
									where f.FieldTypeID = {keyFields.FirstOrDefault().FieldTypeId} and f.FormattedValue like @filter_{parameterIdx}
									option(recompile)";
			}

			this.tempTableInfo.TempTableQuery += @$"	

				drop table if exists #advanced_filter_{parameterIdx}
				create table #advanced_filter_{parameterIdx} (AssetId int)

				insert into #advanced_filter_{parameterIdx}
				select A.Id 
				from Asset A
				inner join #assets_hierarchy ah on ah.AssetId = a.ID
				inner join {filteredTable} fh on ah.{matchField} = fh.assetid";

			if (sqlParamsRef != null)
			{
				sqlParamsRef.Add($"@filter_{parameterIdx}", value);
			}
		}
	
		public void LoadFieldType(FieldType ft, IReadOnlyList<string> fieldColumns)
		{
			fieldType = ft;

			if (fieldColumns != null)
			{
				fieldColumn = fieldColumns.FirstOrDefault(x => x.Contains($"F" + fieldType.ID));
			}

			if (fieldColumn == null && fieldColumns != null)
			{
				fieldColumn = fieldColumns.FirstOrDefault(x => x.ToLowerInvariant().Contains($"[{fieldType.Name.ToLowerInvariant()}]"));
			}

			fieldValueValidator = GetValueValidator();
		}

		public AdvancedFilterTempTableInfo GetTempTableFilterData()
		{
			return this.tempTableInfo;
		}

	}
}
