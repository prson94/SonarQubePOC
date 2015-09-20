using d360.core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Name = "list", Namespace = constants.NAMESPACE)]
    public class EditableFieldLookupList : List<EditableFieldLookupItem> { }
}