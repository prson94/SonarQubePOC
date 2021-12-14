using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.entities.Membership;
using d360.core.enums;
using System;
using System.Collections.Generic;

namespace d360.web.Models
{
    public class AssetInsertsExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return //new AssetInserts() {
                new AssetInsert
                {
                    ExecutionItemUid = Guid.Empty,
                    ParentUid = Guid.Empty,
                    Fields = new Dictionary<string, string>() {
                        { "MyApiFieldName1", "My Field value" },
                        { "MyApiFieldName2", "My Field value" }
                    }
                };
            //};
        }
    }

    public class AssetTypeInsertExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new AssetTypeUpsert
            {
                Name = "My asset type name",
                Class = AssetTypeClass.BusinessAsset,
                Description = "A meaningful description of what this asset type is.",
                AutoDisplayDescription = false,
                DisplayFormat = "{Name}",
                Hierarchy = new HierarchyInsert { MaximumDepth = 1, PredicateUid = Guid.Empty },
                IconStyle = new IconStyleInsert { BackColor = "#000", ForeColor = "#FFF", Icon = "fa-database" },
                ParentUid = Guid.Empty,
                Notes = "Notes about usage or any other topic.",
                UseAsTransformation = false,
                AutoDisplayParent = true,
                CanEditParent = true
            };
        }
    }

    public class AssetTypeUpdateExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new AssetTypeUpsert
            {
                Uid = Guid.NewGuid(),
                Name = "My asset type name",
                Class = AssetTypeClass.BusinessAsset,
                Description = "A meaningful description of what this asset type is.",
                AutoDisplayDescription = false,
                DisplayFormat = "{Name}",
                Hierarchy = new HierarchyInsert { MaximumDepth = 1, PredicateUid = Guid.Empty },
                IconStyle = new IconStyleInsert { BackColor = "#000", ForeColor = "#FFF", Icon = "fa-database" },
                ParentUid = Guid.Empty,
                Notes = "Notes about usage or any other topic.",
                UseAsTransformation = false,
                AutoDisplayParent = true
            };
        }
    }

    public class AssetUpdatesExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return //new AssetUpdates() {
                new AssetUpdate
                {
                    ExecutionItemUid = Guid.Empty,
                    Uid = Guid.Empty,
                    ParentUid = Guid.Empty,
                    Fields = new Dictionary<string, string>() {
                        { "MyApiFieldName1", "My Field value" },
                        { "MyApiFieldName2", "My Field value" }
                    }
                };
            //};
        }
    }

    public class UpdateGroupExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return
                new UpdateGroup
                {
                    Uid = Guid.Empty,
                    Name = "Name",
                    Description = "Description",
                    PrimaryOwnerUid = Guid.Empty,
                    SecondaryOwnerUid = Guid.Empty
                };
        }
    }


    public class AddGroupExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return
                new AddGroup
                {
                    Name = "Name",
                    Description = "Description",
                    PrimaryOwnerUid = Guid.Empty,
                    SecondaryOwnerUid = Guid.Empty
                };
        }
    }

    public class InsertUserToGroupExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return
                new InsertUserToGroup { Uid = Guid.Empty };
        }
    }

    // TODO: update it
    public class FavoriteApiModelExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new FavoriteApiModel
            {
                Route = "asset/" + Guid.Empty.ToString()
            };
        }
    }


    #region Asset Browser

    public class GetAssetLineagePostModelExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new AssetBrowserApiHopRequestModel
            {
                Direction = AssetBrowserApiHopDirection.Both,
                Hops = 3,
                PredicateUid = Guid.Empty,
                Assets = new List<AssetBrowserApiHopAssetRequestModel>() { new AssetBrowserApiHopAssetRequestModel { Uid = Guid.Empty } }
            };
        }
    }

    #endregion

    #region RelationshipType Example
    public class RelationshipTypeInsertExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new RelationshipTypeInsert
            {
                ExecutionItemUid = Guid.Empty,
                PredicateUid = Guid.Empty,
                SubjectUid = Guid.Empty,
                ObjectUid = Guid.Empty,
                SubjectCardinality = core.enums.Cardinality.Many,
                ObjectCardinality = core.enums.Cardinality.Many
            };

        }
    }

    public class RelationshipTypeUpdateExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new RelationshipTypeUpdate
            {
                ExecutionItemUid = Guid.Empty,
                Uid = Guid.Empty,
                PredicateUid = Guid.Empty,
                SubjectCardinality = core.enums.Cardinality.Many,
                ObjectCardinality = core.enums.Cardinality.Many
            };

        }
    }

    public class RelationshipTypeDeleteExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new RelationshipTypeDelete
            {
                ExecutionItemUid = Guid.Empty,
                Uid = Guid.Empty,
                Cascade = false
            };
        }
    }


    #endregion
    #region Workflow Type Examples



    #endregion
    #region Data Quality Examples
    public class DataQualityUpdateExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new DataQualityUpdateModel
            {
                ExecutionItemUid = Guid.Empty,
                EvaluatedAssetUid = Guid.Empty,
                RunDate = "yyyy-MM-dd HH:mm:ss",
                PassCount = 0,
                FailCount = 0
            };
        }
    }

    public class DataQualityDeleteExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new DataQualityDeleteModel
            {
                ExecutionItemUid = Guid.Empty,
                Uid = Guid.Empty,
                OwningAssetUid = Guid.Empty,
                EvaluatedAssetUid = Guid.Empty,
                RunDateStart = "yyyy-MM-dd HH:mm:ss",
                RunDateEnd = "yyyy-MM-dd HH:mm:ss",
                EffectiveDateStart = "yyyy-MM-dd",
                EffectiveDateEnd = "yyyy-MM-dd"
            };
        }
    }

    public class DataQualityInsertExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new DataQualityInsertModel
            {
                ExecutionItemUid = Guid.Empty,
                OwningAssetUid = Guid.Empty,
                EvaluatedAssetUid = Guid.Empty,
                EffectiveDate = "yyyy-MM-dd",
                RunDate = "yyyy-MM-dd HH:mm:ss",
                PassCount = 0,
                FailCount = 0
            };
        }
    }

    #endregion

    #region Membership Examples

    public class UserPostExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new
            {
                Username = "user@example.com",
                FirstName = "John",
                LastName = "Smith",
                Password = "xxxxxx",
                IsAdministrator = false,
                ExecutionItemUid = Guid.Empty,
                Fields = new Dictionary<string, string>()
                {
                    { "MyApiFieldName1", "My Field value" },
                    { "MyApiFieldName2", "My Field value" }
                }
            };
        }
    }

    public class UserPutExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new
            {
                uid = Guid.Empty,
                Username = "user@example.com",
                FirstName = "John",
                LastName = "Smith",
                Password = "xxxxxx",
                IsAdministrator = false,
                ExecutionItemUid = Guid.Empty,
                State = "Active|Inactive|Deleted",
                Fields = new Dictionary<string, string>()
                {
                    { "MyApiFieldName1", "My Field value" },
                    { "MyApiFieldName2", "My Field value" }
                }
            };
        }
    }

    public class DeleteGroupExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return
                new DeleteGroupModel
                {
                    Uid = Guid.Empty
                };
        }
    }

    public class DeleteUserExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return
                new DeleteUserModel
                {
                    Uid = Guid.Empty
                };
        }
    }

    #endregion

    #region Responsibilities examples

    public class ResponsibilityTypeAllocationExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new ResponsibilityTypeAllocationInsertModel
            {
                AssetTypeUid = Guid.Empty,
                Permissions = new List<int> { 1, 2, 4, 8 }
            };
        }
    }
    public class ResponsibilitiesDeleteExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return
                new ResponsibilityOverrideDeleteModel() { ResourceUid = Guid.Empty };
        }
    }
    #endregion

    #region Export Template Examples

    public class ExportTemplateUpsertExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new AssetTypeExportTemplateUpsertRequest
            {
                Name = "Export Template Name",
                Description = "A meaningful description of what this Export Template.",
                UsageNotes = "string",
                IncludeFieldTypes = new string[] { "Name" },
                ExportViewType = ExportView.None
            };
        }
    }

    #endregion
}