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
    public class TagApiModel
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember, StringLength(250)]
        public string Value { get; set; }
        [DataMember]
        public Guid? CreatedByUid { get; set; }
        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public Guid? UpdatedByUid { get; set; }
        [DataMember]
        public DateTime UpdatedOn { get; set; }
    }    
}
