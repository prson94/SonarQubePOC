using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), NotMapped]
    public class CommentCount : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public string CommentTypeName { get; set; }

        [DataMember]
        public int Count { get; set; }

        [DataMember]
        public CommentType CommentType { get; set; }

        #endregion
    }
}
