using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using d360.model.helpers.filters.program;

namespace d360.model.helpers.filters
{
	public class ChildFilterToken : FilterBaseToken, IFilterToken, ITempTableFilter
	{
		private readonly IFieldValueValidator fieldValueValidator;
		private string _tempFilterTableSQL = "";

		public ChildFilterToken(IFilterDataProvider fdp, string field, string op, object value, DefaultFilter @default, int? paramIdx = null)
		{
			dataProvider = fdp;
			parameterIdx = paramIdx ?? -1;
			this.field = field;
			@operator = op;
			this.value = value;
			defaultFilter = @default;

			if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
			{
				IsNullValue = true;
			}

			fieldValueValidator = GetValueValidator();
		}

		public string GetSqlExpression(Dictionary<string, object> sqlParams)
		{
			sqlParamsRef = sqlParams;
			value = value.ToString().ToLower(CultureInfo.InvariantCulture);

			var valueValidation = fieldValueValidator.CheckValue(value, field, @operator);

			if (!valueValidation.Status)
			{
				throw new FormatException(valueValidation.Message);
			}

			value = valueValidation.UpdatedValue;
			value = value.ToString().Trim('\'');

			if (@operator.ToLowerInvariant() == "eq")
			{
				stringBuilder.Append("exists(select top 1 1 from #tempParentFilter tpf where tpf.AssetId = a.ID)");
			}
			else
			{
				stringBuilder.Append("not exists(select top 1 1 from #tempParentFilter tpf where tpf.AssetId = a.ID)");
			}
			var propName = $"filter_parent_asset_{parameterIdx}";
			sqlParamsRef.Add("@" + propName, value);

			SetTempTableQuery(propName);

			return stringBuilder.ToString();
		}

		public AdvancedFilterTempTableInfo GetTempTableFilterData()
		{
			return new AdvancedFilterTempTableInfo
			{
				ApiName = "parentuid",
				TempTableQuery = _tempFilterTableSQL,
				TempTableJoin = "inner join #tempParentFilter tpf on tpf.assetid = a.id"
			};
		}

		private void SetTempTableQuery(string propName)
		{
			StringBuilder tempTableBuilder = new StringBuilder();
			tempTableBuilder.AppendLine(@"
						drop table if exists #tempParentFilter
						create table #tempParentFilter (AssetId bigint);");

			var targetPropName = $"@targetAssetId_for_{propName}";

			tempTableBuilder.AppendLine($@"
								declare {targetPropName} int = (select top 1 Id from Asset where uid = cast(@{propName} as uniqueidentifier));

								insert into #tempParentFilter
								select A.Id
								from Asset A
								inner
								join [Intersect] I on I.ObjectAssetID = A.ID and I.SubjectAssetId = {targetPropName}
								inner join[IntersectType] IT on IT.Id = I.IntersectTypeId
								inner join[Predicate] P on P.ID = IT.PredicateID
								where A.AssetTypeID = @Assettypeid and P.Type = 3");
		
			_tempFilterTableSQL = tempTableBuilder.ToString();
		}
	}
}
