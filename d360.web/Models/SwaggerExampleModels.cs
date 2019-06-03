using d360.core.entities;
using d360.core.entities.Workflow;
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
            return  new AssetTypeInsert
                 {
                    Uid = Guid.Empty,
                    Name = String.Empty,
                    Class = core.enums.AssetTypeClass.Glossary,
                    Description = String.Empty,
                    AutoDisplayDescription = true,
                    DisplayFormat = "{Name}",
                    Hierarchy = new HierarchyInsert
                    {
                        MaximumDepth =3,
                        PredicateUid = Guid.Empty
                    },
                    IconStyle = new IconStyleInsert
                    {
                        BackColor= "#000",
                        ForeColor= "#FFF"
                    },
                    ParentUid = Guid.Empty,
                    Notes = String.Empty
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

    #region Workflow Type Examples

    public class WorkWorkflowTypeApiViewModelExample : IExamplesProvider
    {
        public object GetExamples()
        {
            return new WorkflowTypeApiViewModel()
            {
                ActionTypeUid = Guid.Empty,
                AssetTypeUid = Guid.Empty,
                RelationshipTypeUid = Guid.Empty,
                Name = string.Empty,
                Description = string.Empty,
                State = core.enums.State.Active,
                ChangeType = core.enums.Workflow.ChangeType.Add,
                PublishedVersionUid = Guid.Empty,
                CreatedOn = DateTime.Now,
                UpdatedOn = DateTime.Now
            };
        }
    }

    #endregion
}