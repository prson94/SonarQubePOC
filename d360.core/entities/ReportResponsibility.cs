using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract]
    public class ReportResponsibility : BaseIntObject, IIntObject
    {
        [DataMember]
        public int ReportID { get; set; }

        [DataMember]
        public int ResponsibilityTypeID { get; set; }
    }
}
