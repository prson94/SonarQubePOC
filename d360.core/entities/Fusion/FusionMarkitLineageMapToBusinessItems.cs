using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class FusionMarkitLineageMapToBusinessItems
    {
        public int MapID { get; set; }
        public int ObjectID { get; set; }
        [Column(TypeName = "varchar"), StringLength(20)]
        public string Object { get; set; }
    }
}
