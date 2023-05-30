using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using d360.core;
using d360.core.entities;
using d360.model.helpers.filters.program;
using Newtonsoft.Json;

namespace d360.model.helpers.filters
{
	public class TempTableFieldToken : FilterBaseToken, IFilterToken, ITempTableFilter
	{
		private IFieldValueValidator fieldValueValidator;
		private readonly AdvancedFilterTempTableInfo tempTableInfo = new AdvancedFilterTempTableInfo();
		private readonly string[] lookupFieldTypes = new[] { "Lookup", "Relationship" };
		private string joinSql = "";
		public TempTableFieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
		{
			dataProvider = fdp;
			parameterIdx = paramIdx ?? -1;
			this.field = field;
			@operator = op;
			this.value = value;

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

			string filterExpression = "";
			if (!IsNullValue)
			{
				filterExpression = NotNullValueExpression();
			}
			else
			{
				filterExpression = UpdateTokenForNullValue();
			}

			string fieldJoin = joinSql;

			if (fieldType.Type == DataType.Score.ToString())
			{
				//requires temp table build in FilterExpressionParser.GetAdvancedFilterTempTableFilters
				fieldJoin = @$"left join #scoreTempValues{fieldType.ID} F{fieldType.ID} on F{fieldType.ID}.AssetId = A.ID";
			}

			List<string> listDataType = new List<string>();
			//add item using add method
			listDataType.Add(DataType.FieldFromRelationship.ToString());
			listDataType.Add(DataType.RefListRelationship.ToString());
			listDataType.Add(DataType.JsonElement.ToString());
			listDataType.Add(DataType.Score.ToString());
			listDataType.Add(DataType.Counter.ToString());
			listDataType.Add(DataType.Tag.ToString());
			listDataType.Add(DataType.Lookup.ToString());
			listDataType.Add(DataType.ComplexRelationLookup.ToString());
			listDataType.Add(DataType.OwnershipLookup.ToString());
			listDataType.Add(DataType.Path.ToString());

			var match = listDataType.Where(x => x.ToLowerInvariant() == fieldType.Type.ToLowerInvariant()).ToList();

			if (match.Count == 0  && fieldJoin.ToLowerInvariant().StartsWith("left join Field ".ToLowerInvariant()))
			{
				this.tempTableInfo.TempTableQuery = @$"
				drop table if exists #advanced_filter_{parameterIdx}
				create table #advanced_filter_{parameterIdx} (AssetId bigint)

				insert into #advanced_filter_{parameterIdx}
				select  F{fieldType.ID}.AssetID 
				from Field F{fieldType.ID}
				where F{fieldType.ID}.FieldTypeID = {fieldType.ID} and {filterExpression}
				option(recompile)";
			}
			else
			{
				this.tempTableInfo.TempTableQuery = @$"
				drop table if exists #advanced_filter_{parameterIdx}
				create table #advanced_filter_{parameterIdx} (AssetId bigint)

				insert into #advanced_filter_{parameterIdx}
				select A.Id 
				from Asset A
				{fieldJoin}
				where a.AssetTypeID = @assettypeid and {filterExpression}
				option(recompile)";
			}

			if (sqlParamsRef != null)
			{
				sqlParamsRef.Add($"@filter_{parameterIdx}", value);
			}

			return $"(A.ID in (select AssetID from #advanced_filter_{parameterIdx}))";
		}

		string UpdateTokenForNullValue()
		{
			if (!new[] { "eq", "ne" }.Contains(@operator))
			{
				throw new FormatException($"NULL value filter can be used only with 'eq' and 'ne' operator!");
			}

			var fieldSql = GetColumnValueSyntax(fieldType.ID);

			if (fieldType.Type == DataType.Text.ToString() || fieldType.Type == DataType.Html.ToString())
			{
				if (@operator == "eq")
				{
					return $"({fieldSql} is null or {fieldSql} = '')";
				}
				else
				{
					return $"({fieldSql} is not null and {fieldSql} <> '')";
				}
			}

			return $"{fieldSql} {FilterHelpers.GetSQLNullOperator(@operator)}";
		}

		string NotNullValueExpression()
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
			bool isListField = lookupFieldTypes.Select(x => x.ToLower()).Contains(fieldType.Type.ToLower());

			if (isListField)
			{
				return LoadListFieldQueryWithTempTables();
			}
			else
			{
				return LoadFieldQueryForTemporaryTables();
			}

		}

		private string LoadFieldQueryForTemporaryTables()
		{
			var fieldSql = GetColumnValueSyntax(fieldType.ID);

			if (@operator == "eq" && fieldType.Type == "Text" && fieldSql.ToLower().Contains("formattedvalue") )
			{
				fieldSql = $"trim({fieldSql})";
			}

			if (@operator == "ct" && fieldType.Type != "Text")
			{
				fieldSql = $"CONVERT(NVARCHAR(max),{fieldSql})";
			}

			if (ConvertToNvarChar)
			{
				fieldSql = $"CONVERT(VARCHAR,{fieldSql},120)";
			}

			if (fieldType.Type == "Score")
			{
				fieldSql = $"CONVERT(DECIMAL(8,3),{fieldSql})";
			}

			return $"{fieldSql} {FilterHelpers.GetSQLOperator(@operator)} @filter_{parameterIdx}";

		}

		public string LoadListFieldQueryWithTempTables()
		{
			if (fieldType.LookupObjectID == null)
			{
				throw new FilterExpressionParserException("Lookup field type is missing LookupObjectID value!");
			}

			bool isFieldFromRel = dataProvider.IsFieldFromRelationship(fieldType.ID);

			string type = fieldType.Type;
			int fieldTypeId = fieldType.ID;
			int fieldTypeIdForLookupValue = fieldType.ID;
			string lookupObjectType = fieldType.LookupObjectType;
			int lookupObjectId = fieldType.LookupObjectID.HasValue ? fieldType.LookupObjectID.Value : 0;
			string defaultValue = fieldType.DefaultValue;
			bool allowAllValue = fieldType.AllowAllValue;
			string valueQueryPart = "Value";
			string filterExpression = "";
			//handle field from relationship list values
			if (isFieldFromRel)
			{
				var lookupFieldType = dataProvider.GetFieldTypeById(fieldType.LookupObjectFieldTypeID);
				fieldTypeIdForLookupValue = lookupFieldType.ID;
				lookupObjectType = lookupFieldType.LookupObjectType;
				lookupObjectId = lookupFieldType.LookupObjectID.HasValue ? lookupFieldType.LookupObjectID.Value : 0;
				defaultValue = lookupFieldType.DefaultValue;
				allowAllValue = lookupFieldType.AllowAllValue;
				valueQueryPart = "FormattedValue";
			}

			if (type == "Lookup")
			{
				if (@operator == "ct")
				{
					filterExpression = $"F{fieldTypeId}.FormattedValue like @filter_{parameterIdx}";
				}
				else
				{

					int lookupValue = dataProvider.GetFieldLookupValue(lookupObjectType, lookupObjectId, fieldTypeIdForLookupValue, value.ToString());

					if (lookupValue <= 0)
					{
						throw new FilterExpressionParserException($"Invalid lookup value '{value}' for field '{field}'");
					}

					if (!isFieldFromRel)
					{
						value = lookupValue.ToString();
					}


					string condition = "in";

					if (@operator == "ne")
					{
						condition = "not in";
					}

					if (!string.IsNullOrEmpty(defaultValue))
					{
						if (fieldType.AllowMultipleValues)
						{
							filterExpression = $"@filter_{parameterIdx} {condition} (select * from string_split(coalesce(F{fieldTypeId}.{valueQueryPart},@defLookupValue{parameterIdx}),','))";
						}
						else
						{
							filterExpression = $"@filter_{parameterIdx} {(condition == "in" ? "=" : "!=")} coalesce(F{fieldTypeId}.{valueQueryPart},@defLookupValue{parameterIdx})";
						}
						sqlParamsRef.Add($"@defLookupValue{parameterIdx}", defaultValue);
					}
					else
					{
						if (fieldType.AllowMultipleValues)
						{
							filterExpression = $"@filter_{parameterIdx} {condition} (select * from string_split(F{fieldTypeId}.{valueQueryPart},','))";
						}
						else
						{
							filterExpression = $"@filter_{parameterIdx} {(condition == "in" ? "=" : "!=")} F{fieldTypeId}.{valueQueryPart}";
						}
					}

					if (allowAllValue)
					{
						filterExpression = $"(F{fieldTypeId}.{valueQueryPart} = '0' or {filterExpression})";
					}
				}
			}
			return filterExpression;
		}

		public void LoadFieldType(FieldType ft, IReadOnlyList<string> fieldColumns, DynamicQueryJoins dynamicQueryJoins)
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

			if (dynamicQueryJoins != null)
			{
				joinSql = dynamicQueryJoins.GetJoinStatementForFieldTypeId(fieldType.ID);
			}

			fieldValueValidator = GetValueValidator();
		}

		public AdvancedFilterTempTableInfo GetTempTableFilterData()
		{
			return this.tempTableInfo;
		}

	}
}
