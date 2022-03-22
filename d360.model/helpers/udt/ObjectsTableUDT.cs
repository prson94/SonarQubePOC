using System;
using System.Collections.Generic;

namespace d360.model.DataAccessLayer.repositories
{
    [UDTName("dbo.ObjectsTable")]
    internal class ObjectsTableUDT : IEquatable<ObjectsTableUDT>
    {
        [UDTOrder(0)]
        public string ObjectType { get; set; }

        [UDTOrder(1)]
        public int ObjectId { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ObjectsTableUDT);
        }

        public bool Equals(ObjectsTableUDT other)
        {
            return other != null &&
                   ObjectType == other.ObjectType &&
                   ObjectId == other.ObjectId;
        }

        public override int GetHashCode()
        {
            int hashCode = 1363435841;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ObjectType);
            hashCode = hashCode * -1521134295 + ObjectId.GetHashCode();
            
            return hashCode;
        }

        public static bool operator ==(ObjectsTableUDT left, ObjectsTableUDT right)
        {
            return EqualityComparer<ObjectsTableUDT>.Default.Equals(left, right);
        }

        public static bool operator !=(ObjectsTableUDT left, ObjectsTableUDT right)
        {
            return !(left == right);
        }
    }
}
