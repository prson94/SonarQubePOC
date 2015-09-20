using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    /// <summary>
    /// Defines what types of artifacts can be assigned as a source for a given responsibility type.
    /// </summary>
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeRelation : BaseObject
    {
        [Key, Column(Order = 1)]
        public int ResponsibilityTypeID { get; set; }

        [Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        public virtual ResponsibilityType ResponsibilityType { get; set; }
    }
}
