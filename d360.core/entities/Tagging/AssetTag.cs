using d360.core.entities.Contracts;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetTag : BaseLongObject, IUIDMetadata,ICreatedMetadata
    {
        [DataMember]
        public Guid? UID { get; set; }
        [DataMember]
        public long AssetID { get; set; }
        [DataMember]
        public int TagID { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }

    }

}
