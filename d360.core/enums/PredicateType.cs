using d360.core.entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace d360.core.enums
{
    public enum PredicateType
    {
        [
            Name("Data Lineage"),
            Description("Allows you to define source paths between objects."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(1, 2)
        ]
        DataLineage = 1,
        [
            Name("Reference Data Lineage"),
            Description("Allow for defining links between reference items across lists."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromRelationshipEditor(false),
            LineageVersionsSupported(2)
        ]
        ReferenceLineage = 2,
        [
            Name("Inter-type Hierarchy"),
            Description("This hierarchy allows for creating a tree structure or hierarchy referencing different asset types at each level."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromRelationshipEditor(false)
        ]
        InterTypeHierarchy = 3,
        [
            Name("Intra-type Hierarchy"),
            Description("This hierarchy allows for creating a tree structure or hierarchy referencing the same asset type at each level."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(false),
            ForceDifferentSubjectObject(false),
            AllowEditFromRelationshipEditor(false)
        ]
        IntraTypeHierarchy = 4,
        [
            Name("User Ownership - NOT USED YET"),
            Description("This allows owners to be associated with owned items."),
            ReadOnly(true),
            AllowIntersectTypeAssignment(false),
            AllowMultiplePredicates(false),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(1)
        ]
        UserOwnership = 5,
        [
            Name("Grammatic Association"),
            Description("Allows you to establish grammatic association between two objects."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromRelationshipEditor(true)
        ]
        Grammar = 6,
        [
            Name("Simple"),
            Description("Allows you to create a simple association between two objects that do not fit into any other functional type, such as lineage."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromRelationshipEditor(true)
        ]
        Simple = 7,
        [
            Name("Mapping"),
            Description("Allows you to create mappings that are used in fusion rules."),
            ReadOnly(true),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(false),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(1, 2)
        ]
        FusionMapping = 8,
        [
            Name("See Also"),
            Description("This type of predicate allows for items to be related together to express similarity between them."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromRelationshipEditor(true)
        ]
        SeeAlso = 9,
        [
            Name("Usage"),
            Description("This type of predicate allows for items to be act as filters within a greater lineage diagram to indicate that only certain paths are used."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(1)
        ]
        Usage = 10,
        [
            Name("Object Ownership"),
            Description("This type of predicate allows for fusion configurations to be owned by glossary-level objects."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromRelationshipEditor(false),
            LineageVersionsSupported(1, 2)
        ]
        ObjectOwnerhip = 11,
        [
            Name("Transformation"),
            Description("This type of predicate enforces relationships to or from an asset whose type is marked as supporting transformations. When configuring relationship types, either the subject or object must be a transformation asset type, but not both."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(3)
        ]
        Transformation = 12,
        [
            Name("Business To Technical"),
            Description("This type of predicate enforces relationships to or from an asset whose type is classified as a Technical Asset. When configuring relationship types, the subject must be a Business asset while the object must be a Technical asset."),
            ReadOnly(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(3)
        ]
        BusinessToTechnical = 13
    }


    public class PredicateTypeInfo : BaseObject
    {
        [DataMember]
        public PredicateType ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [JsonIgnore]
        public bool AllowIntersectTypeAssignment { get; set; }

        [JsonIgnore]
        public bool AllowMultiplePredicates { get; set; }

        [JsonIgnore]
        public bool AllowDifferentSubjectObject { get; set; }

        [JsonIgnore]
        public bool ForceDifferentSubjectObject { get; set; }

        [JsonIgnore]
        public bool AllowEditFromRelationshipEditor { get; set; }

        [JsonIgnore]
        public bool ReadOnly { get; set; }

        [JsonIgnore]
        public int[] LineageVersionsSupported { get; set; }
    }

    public class PredicateTypeApiViewModel : BaseObject
    {
        [DataMember]
        public PredicateType Type { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
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

        public static bool IsSystemReserved(this PredicateType type)
        {
            return !type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<AllowIntersectTypeAssignmentAttribute>().Allowed;
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
                        ID = (PredicateType)Enum.Parse(typeof(PredicateType), tm.Name),
                        AllowIntersectTypeAssignment = ((AllowIntersectTypeAssignmentAttribute)tm.GetCustomAttribute(typeof(AllowIntersectTypeAssignmentAttribute))).Allowed,
                        AllowMultiplePredicates = ((AllowMultiplePredicatesAttribute)tm.GetCustomAttribute(typeof(AllowMultiplePredicatesAttribute))).Allowed,
                        AllowDifferentSubjectObject = ((AllowDifferentSubjectObjectAttribute)tm.GetCustomAttribute(typeof(AllowDifferentSubjectObjectAttribute))).Allowed,
                        AllowEditFromRelationshipEditor = ((AllowEditFromRelationshipEditorAttribute)tm.GetCustomAttribute(typeof(AllowEditFromRelationshipEditorAttribute))).Allowed,
                        ForceDifferentSubjectObject = ((ForceDifferentSubjectObjectAttribute)tm.GetCustomAttribute(typeof(ForceDifferentSubjectObjectAttribute))).Allowed,
                        ReadOnly = ((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly,
                        LineageVersionsSupported = tm.IsDefined(typeof(LineageVersionsSupportedAttribute), false) ? 
                                                    ((LineageVersionsSupportedAttribute)tm.GetCustomAttribute(typeof(LineageVersionsSupportedAttribute))).Versions :
                                                    new int[4] { 0, 1, 2, 3 }, //0 accounts for unit tests
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
                    ID = type,
                    AllowIntersectTypeAssignment = ((AllowIntersectTypeAssignmentAttribute)t.GetCustomAttribute(typeof(AllowIntersectTypeAssignmentAttribute))).Allowed,
                    AllowMultiplePredicates = ((AllowMultiplePredicatesAttribute)t.GetCustomAttribute(typeof(AllowMultiplePredicatesAttribute))).Allowed,
                    AllowDifferentSubjectObject = ((AllowDifferentSubjectObjectAttribute)t.GetCustomAttribute(typeof(AllowDifferentSubjectObjectAttribute))).Allowed,
                    ForceDifferentSubjectObject = ((ForceDifferentSubjectObjectAttribute)t.GetCustomAttribute(typeof(ForceDifferentSubjectObjectAttribute))).Allowed,
                    ReadOnly = ((ReadOnlyAttribute)t.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly,
                    AllowEditFromRelationshipEditor = ((AllowEditFromRelationshipEditorAttribute)t.GetCustomAttribute(typeof(AllowEditFromRelationshipEditorAttribute))).Allowed,
                    LineageVersionsSupported = t.IsDefined(typeof(LineageVersionsSupportedAttribute), false) ?
                                                    ((LineageVersionsSupportedAttribute)t.GetCustomAttribute(typeof(LineageVersionsSupportedAttribute))).Versions :
                                                    new int[4] { 0, 1, 2, 3 }, //0 accounts for unit tests
                };
        }
    }
}
