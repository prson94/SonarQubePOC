using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), NotMapped]
    public class CommentVote : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int CommentID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public int Vote { get; set; }

        #endregion

    }
}
