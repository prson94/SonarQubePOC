using d360.core.entities;
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
                    Class = core.enums.AssetTypeClass.Glossary.ToString(),
                    Description = String.Empty,
                    AutoDisplayDescription = true,
                    DisplayFormat = String.Empty,
                    Hierarchy = new HierarchyInsert
                    {
                        MaximumDepth =3,
                        PredicateUid = Guid.Empty
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
                    Fields = new Dictionary<string, string>() {
                        { "MyApiFieldName1", "My Field value" },
                        { "MyApiFieldName2", "My Field value" }
                    }
                };
            //};
        }
    }
}