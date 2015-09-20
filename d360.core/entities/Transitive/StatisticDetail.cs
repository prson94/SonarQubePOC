using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class StatisticDetail: BaseObject
    {
        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        public string Slug { get; set; }
        
        [DataMember]
        public string Score { get; set; }
    }
}
