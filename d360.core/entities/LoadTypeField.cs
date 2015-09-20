using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadTypeField : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int LoadTypeID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public string LookupObjectType { get; set; }

        [DataMember]
        public int? LookupObjectID { get; set; }

        [DataMember]
        public string LookupFieldName { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("LoadTypeID")]
        public virtual LoadType LoadType { get; set; }
    }
}
