using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum PredicateType
    {
        [
            Name("Lineage"), 
            Graph("Lineage"), 
            Description("Allows you to define source paths between objects."), 
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true), 
            AllowDifferentSubjectObject(true), 
            ForceDifferentSubjectObject(false)
        ]
        Lineage = 1,
        [
            Name("Source To Target"), 
            Graph("Lineage"), 
            Description("The most common mapping that allows you to set sources and targets across types contained in the system."), 
            ReadOnly(true),
            AllowIntersectTypeAssignment(false),
            AllowMultiplePredicates(true), 
            AllowDifferentSubjectObject(true), 
            ForceDifferentSubjectObject(false)
        ]
        SourceToTarget = 2,
        [
            Name("Type Hierarchy"), 
            Graph("Glossary"), 
            Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level."), 
            ReadOnly(true),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(false), 
            AllowDifferentSubjectObject(false), 
            ForceDifferentSubjectObject(false)
        ]
        TypeHierarchy = 3,
        [
            Name("Group Hierarchy"), 
            Graph("Glossary"), 
            Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level."), 
            ReadOnly(true),
            AllowIntersectTypeAssignment(false),
            AllowMultiplePredicates(false), 
            AllowDifferentSubjectObject(true), 
            ForceDifferentSubjectObject(true)
        ]
        GroupHierarchy = 4,
        [
            Name("Parent Child Hierarchy"), 
            Graph("Glossary"), 
            Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level."), 
            ReadOnly(true), 
            AllowIntersectTypeAssignment(false),
            AllowMultiplePredicates(false), 
            AllowDifferentSubjectObject(true), 
            ForceDifferentSubjectObject(true)
        ]
        ParentChildHierarchy = 5,
        [
            Name("Synonym"), 
            Graph("Glossary"), 
            Description("Allows you to establish synonyms between two objects that are synonyms of each other."), 
            ReadOnly(true),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(false), 
            AllowDifferentSubjectObject(true), 
            ForceDifferentSubjectObject(false)
        ]
        Synonym = 6,
        [
            Name("Simple"), 
            Graph("Glossary"), 
            Description(""), 
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true), 
            AllowDifferentSubjectObject(true), 
            ForceDifferentSubjectObject(false)
        ]
        Simple = 7,
        [
            Name("Mapping"),
            Graph("Fusion"),
            Description("Allows you to create mappings that are used in fusion rules."),
            ReadOnly(true),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(false),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true)
        ]
        FusionMapping = 8,
        [
            Name("See Also"),
            Graph("Glossary"),
            Description("This type of predicate allows for items to be related together to express similarity between them."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false)
        ]
        SeeAlso = 9
    }

    public class PredicateTypeInfo
    {
        public PredicateType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Graph { get; set; }
        public bool AllowIntersectTypeAssignment { get; set; }
        public bool AllowMultiplePredicates { get; set; }
        public bool AllowDifferentSubjectObject { get; set; }
        public bool ForceDifferentSubjectObject { get; set; }
        public bool ReadOnly { get; set; }
    }

    public static class PredicateTypeExtensions
    {
        public static string GetDisplayName(this PredicateType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetName(this PredicateType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this PredicateType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<PredicateTypeInfo> GetAsList(this PredicateType type)
        {
            var list = new List<PredicateTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (!((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly)
                {
                    list.Add(new PredicateTypeInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        Graph = ((GraphAttribute)tm.GetCustomAttribute(typeof(GraphAttribute))).Graph,
                        ID = (PredicateType)Enum.Parse(typeof(PredicateType), tm.Name),
                        AllowIntersectTypeAssignment = ((AllowIntersectTypeAssignmentAttribute)tm.GetCustomAttribute(typeof(AllowIntersectTypeAssignmentAttribute))).Allowed,
                        AllowMultiplePredicates = ((AllowMultiplePredicatesAttribute)tm.GetCustomAttribute(typeof(AllowMultiplePredicatesAttribute))).Allowed,
                        AllowDifferentSubjectObject = ((AllowDifferentSubjectObjectAttribute)tm.GetCustomAttribute(typeof(AllowDifferentSubjectObjectAttribute))).Allowed,
                        ForceDifferentSubjectObject = ((ForceDifferentSubjectObjectAttribute)tm.GetCustomAttribute(typeof(ForceDifferentSubjectObjectAttribute))).Allowed,
                        ReadOnly = ((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }

        public static PredicateTypeInfo AsInfoModel(this PredicateType type)
        {
            var t = type.GetType().GetMember(type.ToString()).First();
            return 
                new PredicateTypeInfo
                {
                    Name = ((NameAttribute)t.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)t.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    Graph = ((GraphAttribute)t.GetCustomAttribute(typeof(GraphAttribute))).Graph,
                    ID = type,
                    AllowIntersectTypeAssignment = ((AllowIntersectTypeAssignmentAttribute)t.GetCustomAttribute(typeof(AllowIntersectTypeAssignmentAttribute))).Allowed,
                    AllowMultiplePredicates = ((AllowMultiplePredicatesAttribute)t.GetCustomAttribute(typeof(AllowMultiplePredicatesAttribute))).Allowed,
                    AllowDifferentSubjectObject = ((AllowDifferentSubjectObjectAttribute)t.GetCustomAttribute(typeof(AllowDifferentSubjectObjectAttribute))).Allowed,
                    ForceDifferentSubjectObject = ((ForceDifferentSubjectObjectAttribute)t.GetCustomAttribute(typeof(ForceDifferentSubjectObjectAttribute))).Allowed,
                    ReadOnly = ((ReadOnlyAttribute)t.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly
                };
        }
    }
}
