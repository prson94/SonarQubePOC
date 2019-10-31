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
    public class AssetTypeStyle : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconBackColor { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconForeColor { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string IconText { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Icon { get; set; }
    }
}
