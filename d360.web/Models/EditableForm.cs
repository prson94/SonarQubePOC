using d360.core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class EditableForm
    {
        public EditableForm()
        {
            FormSize = EditableForm.FormSize_Medium;
        }
        internal static string FormSize_Small = "small";
        internal static string FormSize_Medium = "medium";
        internal static string FormSize_Large = "large";

        public string Context { get; set; }
        public string FormTitle { get; set; }
        public string FormDescription { get; set; }
        public string FieldUri { get; set; }
        public string FormUri { get; set; }
        public string FormMethod { get; set; }
        public string FormSize { get; set; }
    }
}