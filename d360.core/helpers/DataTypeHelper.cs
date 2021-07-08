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
        public static List<string> GetNotAllowedToUpdateViaAssetApi(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.Path.ToString(),
                DataType.RefListRelationship.ToString(),
                DataType.Score.ToString(),
                DataType.Tag.ToString()

            };

            return types;
        }

        public static List<string> GetNotAllowedInExport(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.RefListRelationship.ToString()
            };

            return types;
        }

        public static List<string> GetNotAllowedInReportingViews(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.RefListRelationship.ToString(),
                DataType.Path.ToString(),
                DataType.Score.ToString(),
            };

            return types;
        }

        public static List<string> GetNotAllowedInFieldFromRelationship(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.JSON.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.Relationship.ToString(),
                DataType.RefListRelationship.ToString(),
                DataType.Tag.ToString(),
                DataType.Score.ToString(),
                DataType.FieldFromRelationship.ToString(),
                DataType.Counter.ToString()
            };

            return types;
        }

        public static List<string> GetNotAllowedInRelationshipLookup(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.JSON.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.Path.ToString(),
                DataType.Relationship.ToString(),
                DataType.RefListRelationship.ToString(),
                DataType.Tag.ToString(),
                DataType.FieldFromRelationship.ToString()
            };

            return types;
        }

        public static List<string> GetComputedFields(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.Path.ToString(),
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.OwnershipLookup.ToString(),
                DataType.RefListRelationship.ToString(),
                DataType.JsonElement.ToString(),
                DataType.Score.ToString(),
                DataType.Counter.ToString()
            };

            return types;   
        }

        public static List<string> GetNonlistableFields(this DataType dt)
        {
            List<string> types = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.File.ToString(),
                DataType.JSON.ToString(),
                DataType.RefListRelationship.ToString()
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
            types.Add(DataType.Tag.ToString());
            types.Add(DataType.Score.ToString());
            types.Add(DataType.FieldFromRelationship.ToString());
            types.Add(DataType.Counter.ToString());

            return types;
        }

        public static List<string> GetNonDisplayFormatFields(this DataType dt)
        {
            List<string> types = dt.GetNonWorkflowConditionFields();
            return types;
        }
    }
}
