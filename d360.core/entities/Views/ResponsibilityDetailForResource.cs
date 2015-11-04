using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityDetailForResource : BaseObject
    {

        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public int ResponsibilityID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public int ObjectTypeID { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }

        [DataMember]
        public string ObjectUrl { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public string ResponsibleObjectType { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public int ResponsibleObjectID { get; set; }

        [DataMember]
        public bool FromGroup { get; set; }

        [DataMember]
        public string Role { get; set; }

        [DataMember]
        public double? CurrentScore { get; set; }

        [DataMember]
        public bool RedFlagged { get; set; }

        #endregion
    }
}
