using System;
using System.Collections.Generic;

namespace igx.functions.Timer
{
    public class RelationshipEqualityComparer : IEqualityComparer<IRelationshipMapping>
    {
        public bool Equals(IRelationshipMapping x, IRelationshipMapping y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return string.Compare(x.Tag, y.Tag, true) == 0 && string.Compare(x.Expression, y.Expression, true) == 0;
        }

        public int GetHashCode(IRelationshipMapping obj)
        {
            int hash = 17;
            hash = hash * 23 + (obj.Expression ?? "").GetHashCode();
            hash = hash * 23 + (obj.Tag ?? "").GetHashCode();
            return hash;
        }
    }

    public interface IRelationshipMapping
    {
        string Expression { get; set; }
        string Tag { get; set; }
        string UserDescription { get; set; }
        RelationshipColumnType ColumnTagType { get; set; }
        RelationshipExpressionType ExpressionValueType { get; set; }
        uint StarTag { get; set; }
        string ConstantName { get; set; }        
    }
}