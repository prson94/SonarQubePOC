using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SourceRule : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string AppliesToObject { get; set; }

        [DataMember]
        public int AppliesToObjectID { get; set; }

        public string AppliesToObjectList { get; set; }

        #endregion
    }
}
