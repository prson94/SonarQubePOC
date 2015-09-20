using d360.core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class EditableField : ReadOnlyField
    {
        public EditableField()
        {
            Items = new List<EditableFieldItem>();
            ReadOnly = false;
            MultiSelect = false;
        }

        [DataMember]
        public string DataUri { get; set; }

        [DataMember]
        public string FieldType { get; set; }

        [DataMember]
        public List<EditableFieldItem> Items { get; set; }

        [DataMember]
        public bool ReadOnly { get; set; }

        [DataMember]
        public bool Required { get; set; }

        [DataMember]
        public bool MultiSelect { get; set; }

        [DataMember]
        public List<FieldValidationModel> Validations { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class FieldValidationModel
    {
        /// <summary>
        /// The error message to display to the user.
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// keyup, blur, focus, change
        /// </summary>
        public string action { get; set; }
        /// <summary>
        /// required; length=3,12; right:0,0; phone; ssn; zipCode; email; inline javascript function
        /// </summary>
        public string rule { get; set; }

        public string regex { get; set; }
    }
}