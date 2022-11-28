using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using d360.core.entities;
using d360.core.enums;

namespace d360.model.helpers.filters
{
	public class RelationshipFieldToken : FilterBaseToken, IFilterToken
	{
		private AssetType assetType { get; set; }

		private IntersectType intersectType { get; set; }

		public RelationshipFieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
		{
			dataProvider = fdp;
			parameterIdx = paramIdx ?? -1;
			this.field = field.Replace("$related:", "");
			@operator = op;
			this.value = value.ToString().Replace("'", "");

			if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
			{
				IsNullValue = true;
			}
		}

		public string GetSqlExpression(Dictionary<string, object> sqlParams)
		{
			if ((assetType == null && !IsNullValue) || intersectType == null)
			{
				throw new MethodAccessException("Method can be used only when Intersect Type and Asset Type are loaded. Use LoadRelationshipData() method before.");
			}

			if (IsNullValue)
			{
				return GetNullSqlExpression(sqlParams);
			}

			sqlParamsRef = sqlParams;
			stringBuilder.Clear();

			if (!new[] { "eq", "ne" }.Contains(@operator))
			{
				throw new FilterExpressionParserException($"Operator '{@operator}' is not valid when filtering relationship. Use 'eq' or 'ne'.");
			}

			var condition = @operator == "eq" ? " exists" : " not exists";
			var filterCond = GetSplitFilterCriteriaRelationship();
			var hasRefList = (intersectType.ObjectClass == AssetTypeClass.Reference && intersectType.ObjectAssetTypeID == 0) || (intersectType.SubjectClass == AssetTypeClass.Reference && intersectType.SubjectAssetTypeID == 0);

			if (!hasRefList)
			{
				AddRelationshipFilterWithGraphTables(condition, filterCond);
			}
			else
			{
				stringBuilder.Append($@"{condition} (select AT.Uid from [IntersectType] IT
				left join [Intersect] I1 on I1.IntersectTypeID = IT.ID and I1.ObjectAssetID = A.ID
				left join [Intersect] I2 on I2.IntersectTypeID = IT.ID and I2.SubjectAssetID = A.ID
				inner join AssetType AT on AT.ID = ISNULL(I1.SubjectAssetTypeID,I2.ObjectAssetTypeID) 
				where IT.Uid = @intersectFilter{parameterIdx} and AT.Uid = @intersectAssetFilter{parameterIdx})");
			}


			sqlParams.Add($"@intersectFilter{parameterIdx}", Guid.Parse(field));
			sqlParams.Add($"@intersectAssetFilter{parameterIdx}", Guid.Parse(ValueAsString));

			return stringBuilder.ToString();
		}

		public string GetNullSqlExpression(Dictionary<string, object> sqlParams)
		{
			sqlParamsRef = sqlParams;
			stringBuilder.Clear();

			if (!new[] { "eq", "ne" }.Contains(@operator))
			{
				throw new FilterExpressionParserException($"Operator '{@operator}' is not valid when filtering relationship. Use 'eq' or 'ne'.");
			}

			sqlParams.Add($"@intersectFilter{parameterIdx}", Guid.Parse(field));

			if (@operator == "ne")
			{
				stringBuilder.Append($@"(
				  exists (
					SELECT top 1 *
					FROM   [Intersect] I
					inner join [IntersectType] IT on IT.Id = I.IntersectTypeId
					WHERE  IT.Uid = @intersectFilter{parameterIdx} AND I.SubjectAssetId = A.Id
				)
				or exists (
					SELECT top 1 *
					FROM   [Intersect] I
					inner join [IntersectType] IT on IT.Id = I.IntersectTypeId
					WHERE  IT.Uid = @intersectFilter{parameterIdx} AND I.ObjectAssetId = A.Id
				)
				)");
			}

			if (@operator == "eq")
			{
				stringBuilder.Append($@"(
				 not exists (
					SELECT top 1 *
					FROM   [Intersect] I
					inner join [IntersectType] IT on IT.Id = I.IntersectTypeId
					WHERE  IT.Uid = @intersectFilter{parameterIdx} AND I.SubjectAssetId = A.Id
				)
				and not exists (
					SELECT top 1 *
					FROM   [Intersect] I
					inner join [IntersectType] IT on IT.Id = I.IntersectTypeId
					WHERE  IT.Uid = @intersectFilter{parameterIdx} AND I.ObjectAssetId = A.Id
				)
				)");
			}

			return stringBuilder.ToString();
		}

		private void AddRelationshipFilterWithGraphTables(string condition, SplitFilterCriteriaRelationship filterCond)
		{
			if (filterCond == SplitFilterCriteriaRelationship.Subject)
			{
				stringBuilder.Append($@"{condition}(SELECT TargetAsset.Uid as TargetAssetId
					FROM  [Intersect] I
					Inner join [IntersectType] IT ON IT.ID = I.IntersectTypeId
					inner join [Asset] TargetAsset on TargetAsset.uid = @intersectAssetFilter{parameterIdx}
					WHERE IT.Uid = @intersectFilter{parameterIdx} AND I.ObjectAssetId = A.Id and I.SubjectAssetId = TargetAsset.Id)");
			}
			else if (filterCond == SplitFilterCriteriaRelationship.Object)
			{
				stringBuilder.Append($@"{condition}(SELECT TargetAsset.Uid as TargetAssetId
					FROM  [Intersect] I
					Inner join [IntersectType] IT ON IT.ID = I.IntersectTypeId
					inner join [Asset] TargetAsset on TargetAsset.uid = @intersectAssetFilter{parameterIdx}
					WHERE IT.Uid = @intersectFilter{parameterIdx} AND I.SubjectAssetId = A.Id and I.ObjectAssetId = TargetAsset.Id)");
			}
			else
			{
				stringBuilder.Append($@"{condition}(SELECT TargetAsset.Uid as TargetAssetId
					FROM  [Intersect] I
					Inner join [IntersectType] IT ON IT.ID = I.IntersectTypeId
					inner join [Asset] TargetAsset on TargetAsset.uid = @intersectAssetFilter{parameterIdx}
					WHERE IT.Uid = @intersectFilter{parameterIdx} AND I.ObjectAssetId = A.Id and I.SubjectAssetId = TargetAsset.Id
					UNION
					SELECT TargetAsset.Uid as TargetAssetId
					FROM  [Intersect] I
					Inner join [IntersectType] IT ON IT.ID = I.IntersectTypeId
					inner join [Asset] TargetAsset on TargetAsset.uid = @intersectAssetFilter{parameterIdx}
					WHERE IT.Uid = @intersectFilter{parameterIdx} AND I.SubjectAssetId = A.Id and I.ObjectAssetId = TargetAsset.Id)");
			}
		}

		private SplitFilterCriteriaRelationship GetSplitFilterCriteriaRelationship()
		{
			if (intersectType.ObjectAssetTypeID == assetType.ID && intersectType.SubjectAssetTypeID == assetType.ID)
			{
				return SplitFilterCriteriaRelationship.Both;
			}

			if (intersectType.ObjectAssetTypeID == assetType.ID)
			{
				return SplitFilterCriteriaRelationship.Object;
			}
			else
			{
				return SplitFilterCriteriaRelationship.Subject;
			}

		}

		public void LoadRelationshipData(IntersectType it, AssetType at)
		{
			intersectType = it;
			assetType = at;
		}
	}
}
