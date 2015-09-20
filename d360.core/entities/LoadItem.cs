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
    public class LoadItem : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int LoadID { get; set; }


        [DataMember]
        public int RowIndex { get; set; }

        
        #endregion

        [IgnoreDataMember, ForeignKey("LoadID")]
        public virtual Load Load { get; set; }

        [IgnoreDataMember, ForeignKey("LoadItemID")]
        public virtual ICollection<LoadItemField> LoadItemFields { get; set; }
    }
}
