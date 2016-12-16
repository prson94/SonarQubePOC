using d360.core.entities.Contracts;
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
    public class RuleMap : BaseObject
    {        
        [DataMember, Key, Column(Order = 1)]
        public int RuleID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string SourceID { get; set; }

        [DataMember]
        public string SourceName { get; set; }

        [DataMember]
        public string SourceURI { get; set; }

        [IgnoreDataMember]
        public virtual Rule Rule { get; set; }
    }
}
