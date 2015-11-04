using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public abstract class ResponsibilityDetailBase : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public string AssigningItemType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int AssigningItemID { get; set; }

        [DataMember]
        public string AssigningItemName { get; set; }

        [DataMember]
        public string AssigningItemUrl { get; set; }

        [DataMember]
        public string AssigningTypeName { get; set; }

        [DataMember]
        public string AssigningIconBackColor { get; set; }

        [DataMember]
        public string AssigningIconForeColor { get; set; }

        [DataMember]
        public string AssigningIconText { get; set; }
        
        [DataMember, Key, Column(Order = 3)]
        public int ResponsibilityID { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public string ObjectType { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string ObjectUrl { get; set; }

        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public string ResponsibleObjectType { get; set; }

        [DataMember]
        public int ResponsibleObjectID { get; set; }

        [DataMember]
        public string ResponsibleObjectName { get; set; }

        [DataMember]
        public string ResponsibleObjectUrl { get; set; }

        [DataMember]
        public string Role { get; set; }

        [DataMember]
        public double? CurrentScore { get; set; }

        [DataMember]
        public string ContextItems { get; set; }

        #endregion
    }
}
