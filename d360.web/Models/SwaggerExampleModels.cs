using d360.core.entities;
using d360.core.entities.Workflow;
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
                CanOwnFusion = false
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
                CanOwnFusion = false
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

    public class InsertUserToGroupExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return
                new InsertUserToGroup
                {
                    UserUids = new List<Guid>() {
                        { Guid.Empty },
                        { Guid.Empty }
                    },

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
}