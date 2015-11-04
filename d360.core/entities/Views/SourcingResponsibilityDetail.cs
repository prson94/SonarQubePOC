using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class SourcingResponsibilityDetail : ResponsibilityDetailBase
    {
        [DataMember]
        public string ResponsibleObjectIconBackColor { get; set; }

        [DataMember]
        public string ResponsibleObjectIconForeColor { get; set; }

        [DataMember]
        public string ResponsibleObjectIconText { get; set; }


        [DataMember]
        public bool Actual { get; set; }
    }
}
