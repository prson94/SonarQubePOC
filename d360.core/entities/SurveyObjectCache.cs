using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SurveyObjectCache : BaseObject
    {
        #region Properties

        [Column(Order = 1), DataMember, Key]
        public int SurveyTypeID { get; set; }

        [Column(Order = 2, TypeName = "varchar"), DataMember, Key, StringLength(25)]
        public string ObjectType { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int ObjectID { get; set; }

        [DataMember]
        public string ReportCache { get; set; }

        public virtual SurveyType SurveyType { get; set; }

        #endregion
    }
}
