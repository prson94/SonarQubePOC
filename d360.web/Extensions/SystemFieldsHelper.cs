using System.Collections.Generic;

using d360.core;
using d360.core.entities;
using d360.web.Models;

using Resources;

namespace d360.web.Extensions
{
    /// <summary>
    /// Helper class which prepares rows with System Fields in all asset types
    /// </summary>
    public static class SystemFieldsHelper
    {

        public static DetailReadOnlyRowModel DetailRowInSystemFieldsForCreatedOnAndUpdatedOn(dynamic querySingleResult)
        {
            if (querySingleResult.UpdatedOn != null)
            {
                return new DetailReadOnlyRowModel
                {
                    columns = 2,
                    FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = FieldInfo.CreatedOn_Name, FieldName = "AssetCreatedOn",
                                        FieldDescription = FieldInfo.CreatedOn_Description, Value = querySingleResult.CreatedOn?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                        DataType = "date" }
                                },
                    SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = FieldInfo.UpdatedOn_Name, FieldName = "AssetUpdatedOn",
                                        FieldDescription = FieldInfo.UpdatedOn_Description, Value = querySingleResult.UpdatedOn.ToString("yyyy-MM-ddTHH:mm:ssZ"), DataType = "date" }
                                },
                    Category = FieldInfo.SystemFieldCategory
                };
            }
            return new DetailReadOnlyRowModel
            {
                columns = 1,
                FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = FieldInfo.CreatedOn_Name, FieldName = "AssetCreatedOn",
                                    FieldDescription = FieldInfo.CreatedOn_Description, Value = querySingleResult.CreatedOn?.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                    DataType = "date" }
                            },
                Category = FieldInfo.SystemFieldCategory
            };
        }

        public static DetailReadOnlyRowModel RowWithUserNameLinkAndLookup(int? resourceId, string fieldName, GlobalReportingResource resource)
        {
            return new DetailReadOnlyRowModel
            {
                columns = 1,
                FirstColumnFields = new List<ReadOnlyField> {
                    new ReadOnlyField {
                        Name = fieldName,
                        FieldName = "ReferenceList",
                        Value = "values",
                        Values = new List<ReadOnlyFieldValue>{
                            new ReadOnlyFieldValue {
                                Value = resource?.FullName ?? "",
                                TooltipType = "Resource",
                                TooltipUrl = $"resource/{resourceId}",
                                HideTooltip = true
                            }
                        },
                        DataType = DataType.Lookup.ToString(),
                        ResourceUid = resource?.Uid
                    }
                },
                Category = FieldInfo.SystemFieldCategory
            };
        }
    }
}
