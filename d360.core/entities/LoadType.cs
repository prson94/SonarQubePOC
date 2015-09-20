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
    public class LoadType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("LoadTypeID")]
        public virtual ICollection<Load> Loads { get; set; }

        [IgnoreDataMember, ForeignKey("LoadTypeID")]
        public virtual ICollection<LoadTypeField> LoadTypeFields { get; set; }

        [IgnoreDataMember, ForeignKey("LoadTypeID")]
        public virtual ICollection<LoadTypeRule> LoadTypeRules { get; set; }
    }
}
