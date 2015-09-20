using d360.core;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class EditableFieldItem
    {
        public EditableFieldItem()
        {
            Selected = false;
        }

        [DataMember]
        public string Text { get; set; }
        
        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string Group2 { get; set; }

        [DataMember]
        public bool Selected { get; set; }

    }
}