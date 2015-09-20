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
    public class Load : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int LoadTypeID { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public byte[] File { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("LoadTypeID")]
        public virtual LoadType LoadType { get; set; }

        [IgnoreDataMember, ForeignKey("LoadItemID")]
        public virtual ICollection<LoadItem> LoadItems { get; set; }
    }
}
