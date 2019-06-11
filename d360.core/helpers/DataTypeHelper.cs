using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.helpers
{
    public static class DataTypeHelper
    {
        public static List<string> GetComputedFields(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.Attribute.ToString(),
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.FilteredLookup.ToString(),
                DataType.FusionLookup.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.RefListRelationship.ToString(),
                DataType.JsonElement.ToString()
            };

            return types;
        }

        public static List<string> GetNonlistableFields(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.Attribute.ToString(),
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.FilteredLookup.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.JSON.ToString()
            };

            return types;
        }

        public static List<string> GetNonWorkflowConditionFields(this DataType dt)
        {
            List<string> types = dt.GetComputedFields();
            types.Add(DataType.JSON.ToString());
            types.Add(DataType.Link.ToString());
            types.Add(DataType.Password.ToString());
            types.Add(DataType.Relationship.ToString());
            return types;
        }

        public static List<string> GetNonDisplayFormatFields(this DataType dt)
        {
            List<string> types = dt.GetNonWorkflowConditionFields();
            return types;
        }
    }
}
