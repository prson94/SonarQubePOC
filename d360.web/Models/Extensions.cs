using d360.core.entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{    
    public static class Extensions
    {
        public static FieldTypeComplexLookupDefinition ParseComplexLookupDefinition(this FieldTypeLookup lookup)
        {
            return JsonConvert.DeserializeObject<FieldTypeComplexLookupDefinition>(lookup.Definition);
        }

        public static FieldTypeOwnershipLookupDefinition ParseOwnershipLookupDefinition(this FieldTypeLookup lookup)
        {
            return JsonConvert.DeserializeObject<FieldTypeOwnershipLookupDefinition>(lookup.Definition);
        }


    }
}