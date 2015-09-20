using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("ApiRawReceivedMessage", Schema = "utility")]
    public class ApiRawReceivedMessage : BaseObject
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }
        
        public string ObjectType { get; set; }

        public int ObjectID { get; set; }

        public DateTime Date { get; set; }

        public string Message { get; set; }

    }
}
