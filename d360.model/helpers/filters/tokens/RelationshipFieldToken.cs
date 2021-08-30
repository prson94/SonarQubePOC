using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class RelationshipFieldToken : FilterBaseToken, IFilterToken
    {
        private AssetType assetType { get; set; }
        private IntersectType intersectType { get; set; }

        public RelationshipFieldToken(IFilterDataProvider fdp, string field, string op, object value, int? paramIdx = null)
        {
            this.dataProvider = fdp;
            parameterIdx = paramIdx ?? -1;
            this.field = field.Replace("$related:", "");
            @operator = op;
            this.value = value.ToString().Replace("'", "");

            if (this.value != null && this.value.ToString().ToLower(CultureInfo.InvariantCulture) == "null")
            {
                this.IsNullValue = true;
            }
        }

        public string GetSqlExpression(Dictionary<string, object> sqlParams)
        {
            if ((assetType == null && !IsNullValue) || intersectType == null)
            {
                throw new MethodAccessException("Method can be used only when Intersect Type and Asset Type are loaded. Use LoadRelationshipData() method before.");
            }

            if (this.IsNullValue)
            {
                return this.GetNullSqlExpression(sqlParams);
            }

            this.sqlParamsRef = sqlParams;
            stringBuilder.Clear();

            if (!new[] { "eq", "ne" }.Contains(@operator))
            {
                throw new FilterExpressionParserException($"Operator '{@operator}' is not valid when filtering relationship. Use 'eq' or 'ne'.");
            }

            var condition = @operator == "eq" ? " exists" : " not exists";

            var filterCond = GetSplitFilterCriteriaRelationship();
            var hasRefList = (intersectType.Object == "ReferenceItemType" && intersectType.ObjectID == 0) || (intersectType.Subject == "ReferenceItemType" && intersectType.SubjectID == 0);

            if (!hasRefList)
            {
                AddRelationshipFilterWithGraphTables(condition, filterCond);
            }
            else
            {
                stringBuilder.Append($@"{condition} (select AT.Uid from [IntersectType] IT
	            left join [Intersect] I1 on I1.IntersectTypeID = IT.ID and I1.Object = A.Object and I1.ObjectId = A.ObjectID
	            left join [Intersect] I2 on I2.IntersectTypeID = IT.ID and I2.Subject = A.Object and I2.SubjectID = A.ObjectID
	            inner join AssetType AT on AT.Object = ISNULL(I1.Subject,I2.Object) and AT.ObjectID = ISNULL(I1.SubjectId, I2.ObjectID)
	            where IT.Uid = @intersectFilter{this.parameterIdx} and AT.Uid = @intersectAssetFilter{this.parameterIdx})");
            }


            sqlParams.Add($"@intersectFilter{this.parameterIdx}", Guid.Parse(field));
            sqlParams.Add($"@intersectAssetFilter{this.parameterIdx}", Guid.Parse(ValueAsString));
            return stringBuilder.ToString();
        }


        public string GetNullSqlExpression(Dictionary<string, object> sqlParams)
        {
            this.sqlParamsRef = sqlParams;
            stringBuilder.Clear();

            if (!new[] { "eq", "ne" }.Contains(@operator))
            {
                throw new FilterExpressionParserException($"Operator '{@operator}' is not valid when filtering relationship. Use 'eq' or 'ne'.");
            }

            sqlParams.Add($"@intersectFilter{this.parameterIdx}", Guid.Parse(field));

            if (@operator == "ne")
            {
                stringBuilder.Append($@"(
				  exists (
                    SELECT top 1 *
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid
                )
                or exists (
                    SELECT top 1 *
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND O.Uid = A.Uid
                )
                )");
            }

            if (@operator == "eq")
            {
                stringBuilder.Append($@"(
				 not exists (
                    SELECT top 1 *
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid
                )
                and not exists (
                    SELECT top 1 *
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND O.Uid = A.Uid
                )
                )");
            }

            return stringBuilder.ToString();
        }


        private void AddRelationshipFilterWithGraphTables(string condition, SplitFilterCriteriaRelationship filterCond)
        {
            if (filterCond == SplitFilterCriteriaRelationship.Subject)
            {
                stringBuilder.Append($@"{condition}(SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx})");
            }
            else if (filterCond == SplitFilterCriteriaRelationship.Object)
            {
                stringBuilder.Append($@"{condition}(SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S - (E) -> O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx})");
            }
            else
            {
                stringBuilder.Append($@"{condition}(SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S <- (E) - O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx}
                    UNION
                    SELECT       O.Uid as TargetAssetId
                    FROM         graph.AssetNode S, graph.AssetEdge E, graph.AssetNode O
                    WHERE        MATCH(S - (E) -> O)  AND IntersectTypeUid = @intersectFilter{this.parameterIdx}
				              AND S.Uid = A.Uid and O.Uid = @intersectAssetFilter{this.parameterIdx})");
            }
        }


        private SplitFilterCriteriaRelationship GetSplitFilterCriteriaRelationship()
        {
            if (intersectType.Object == assetType.Object && intersectType.ObjectID == assetType.ObjectID
               && intersectType.Subject == assetType.Object && intersectType.SubjectID == assetType.ObjectID)
            {
                return SplitFilterCriteriaRelationship.Both;
            }
            if (intersectType.Object == assetType.Object && intersectType.ObjectID == assetType.ObjectID)
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
            this.intersectType = it;
            this.assetType = at;
        }
    }
}
