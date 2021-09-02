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
            Category("Lineage"),
            Name("Simple Data Lineage"),
            Description("Allows you to define simple data lineage relationships."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.FusionAttribute, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.FusionAttribute, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset),
            AllowIntersectTypeAsSubject(true)
        ]
        DataLineage = 1,
        [
            Category("Data Quality"),
            Name("Evaluation"),
            Description("Used within data quality scoring to determine which assets should be included as officially being evaluated by a rule."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.Rule),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset),
            AllowIntersectTypeAsSubject(false)
        ]
        Evaluation = 2,
        [
            Category("Ancestry"),
            Name("Inter-type Hierarchy"),
            Description("This hierarchy allows for creating a tree structure or hierarchy referencing different asset types at each level."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(true),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(false),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.FusionAttribute, AssetTypeClass.TechnicalAsset),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.FusionAttribute, AssetTypeClass.TechnicalAsset),
            AllowIntersectTypeAsSubject(false)
        ]
        InterTypeHierarchy = 3,
        [
            Category("Ancestry"),
            Name("Intra-type Hierarchy"),
            Description("This hierarchy allows for creating a tree structure or hierarchy referencing the same asset type at each level."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(true),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(false),
            ForceDifferentSubjectObject(false),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(false),
            SubjectAssetClassesSupported(AssetTypeClass.Model, AssetTypeClass.Policy),
            ObjectAssetClassesSupported(AssetTypeClass.Model, AssetTypeClass.Policy),
            AllowIntersectTypeAsSubject(false)
        ]
        IntraTypeHierarchy = 4,
        [
            Category("General"),
            Name("Grammatic Association"),
            Description("Allows you to establish grammatic association between two objects."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.TechnicalAsset),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.TechnicalAsset),
            AllowIntersectTypeAsSubject(false)
        ]
        Grammar = 6,
        [
            Category("General"),
            Name("Simple"),
            Description("Allows you to create a simple association between two objects that do not fit into any other functional type, such as lineage."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference, AssetTypeClass.User),
            AllowIntersectTypeAsSubject(false)
        ]
        Simple = 7,
        [
            Category("General"),
            Name("See Also"),
            Description("This type of predicate allows for items to be related together to express similarity between them."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(false),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference),
            AllowIntersectTypeAsSubject(true)
        ]
        SeeAlso = 9,
        [
            Category("Lineage"),
            Name("Transformation"),
            Description("This type of predicate enforces relationships to or from an asset whose type is marked as supporting transformations. When configuring relationship types, either the subject or object must be a transformation asset type, but not both."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(3),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset),
            AllowIntersectTypeAsSubject(false)
        ]
        Transformation = 12,
        [
            Category("Lineage"),
            Name("Business To Technical"),
            Description("This type of predicate enforces relationships to or from an asset whose type is classified as a Technical Asset. When configuring relationship types, the subject must be a Business asset while the object must be a Technical asset."),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(3),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule),
            ObjectAssetClassesSupported(AssetTypeClass.TechnicalAsset),
            AllowIntersectTypeAsSubject(false)
        ]
        BusinessToTechnical = 13,
        [
            Category("General"),
            Name("Semantic Relation"),
            Description(""),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(true),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(false),
            ForceDifferentSubjectObject(false),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            LineageVersionsSupported(3),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.TechnicalAsset),
            AllowIntersectTypeAsSubject(false)
        ]
        SemanticRelation = 14,
        [
            Category(""),
            Name("Diagram"),
            Description(""),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Rule, AssetTypeClass.Policy, AssetTypeClass.Model),
            ObjectAssetClassesSupported(AssetTypeClass.Diagram),
            AllowIntersectTypeAsSubject(false)
        ]
        Diagram = 15,
        [
            Category(""),
            Name("Diagram Use"),
            Description(""),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.Diagram),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Rule, AssetTypeClass.Policy, AssetTypeClass.Model),
            AllowIntersectTypeAsSubject(false)
        ]
        DiagramUse = 16,
        [
            Category(""),
            Name("Diagram Reference"),
            Description(""),
            ReadOnly(false),
            SingleRelationshipByFunctionalType(false),
            AllowIntersectTypeAssignment(true),
            AllowMultiplePredicates(true),
            AllowDifferentSubjectObject(true),
            ForceDifferentSubjectObject(true),
            AllowEditFromPredicateEditor(true),
            AllowEditFromRelationshipEditor(true),
            SubjectAssetClassesSupported(AssetTypeClass.Diagram),
            ObjectAssetClassesSupported(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Rule, AssetTypeClass.Policy, AssetTypeClass.Model),
            AllowIntersectTypeAsSubject(false)
        ]
        DiagramReference = 17
    }


    public class PredicateTypeInfo : BaseObject
    {
        [DataMember]
        public PredicateType ID { get; set; }

        [DataMember]
        public string Category { get; set; }

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
        public bool AllowEditFromPredicateEditor { get; set; }

        [JsonIgnore]
        public bool AllowEditFromRelationshipEditor { get; set; }

        [JsonIgnore]
        public bool AllowIntersectTypeAsSubject { get; set; }

        [JsonIgnore]
        public bool SingleRelationshipByFunctionalType { get; set; }

        [JsonIgnore]
        public bool ReadOnly { get; set; }

        [JsonIgnore]
        public bool Obsolete { get; set; }

        [JsonIgnore]
        public int[] LineageVersionsSupported { get; set; }

        [JsonIgnore]
        public AssetTypeClass[] SubjectAssetClassesSupported { get; set; }

        [JsonIgnore]
        public AssetTypeClass[] ObjectAssetClassesSupported { get; set; }
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
                        Category = ((CategoryAttribute)tm.GetCustomAttribute(typeof(CategoryAttribute))).Category,
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (PredicateType)Enum.Parse(typeof(PredicateType), tm.Name),
                        AllowIntersectTypeAssignment = ((AllowIntersectTypeAssignmentAttribute)tm.GetCustomAttribute(typeof(AllowIntersectTypeAssignmentAttribute))).Allowed,
                        AllowMultiplePredicates = ((AllowMultiplePredicatesAttribute)tm.GetCustomAttribute(typeof(AllowMultiplePredicatesAttribute))).Allowed,
                        AllowDifferentSubjectObject = ((AllowDifferentSubjectObjectAttribute)tm.GetCustomAttribute(typeof(AllowDifferentSubjectObjectAttribute))).Allowed,
                        AllowEditFromPredicateEditor = ((AllowEditFromPredicateEditorAttribute)tm.GetCustomAttribute(typeof(AllowEditFromPredicateEditorAttribute))).Allowed,
                        AllowEditFromRelationshipEditor = ((AllowEditFromRelationshipEditorAttribute)tm.GetCustomAttribute(typeof(AllowEditFromRelationshipEditorAttribute))).Allowed,
                        AllowIntersectTypeAsSubject = ((AllowIntersectTypeAsSubjectAttribute)tm.GetCustomAttribute(typeof(AllowIntersectTypeAsSubjectAttribute))).Allowed,
                        SingleRelationshipByFunctionalType = ((SingleRelationshipByFunctionalTypeAttribute)tm.GetCustomAttribute(typeof(SingleRelationshipByFunctionalTypeAttribute))).Allowed,
                        ForceDifferentSubjectObject = ((ForceDifferentSubjectObjectAttribute)tm.GetCustomAttribute(typeof(ForceDifferentSubjectObjectAttribute))).Allowed,
                        ReadOnly = ((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly,
                        LineageVersionsSupported = tm.IsDefined(typeof(LineageVersionsSupportedAttribute), false) ?
                                                    ((LineageVersionsSupportedAttribute)tm.GetCustomAttribute(typeof(LineageVersionsSupportedAttribute))).Versions :
                                                    new int[4] { 0, 1, 2, 3 }, //0 accounts for unit tests,
                        SubjectAssetClassesSupported = ((SubjectAssetClassesSupportedAttribute)tm.GetCustomAttribute(typeof(SubjectAssetClassesSupportedAttribute))).Classes,
                        ObjectAssetClassesSupported = ((ObjectAssetClassesSupportedAttribute)tm.GetCustomAttribute(typeof(ObjectAssetClassesSupportedAttribute))).Classes,
                        Obsolete = tm.IsDefined(typeof(ObsoleteAttribute), false)
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
                    SingleRelationshipByFunctionalType = ((SingleRelationshipByFunctionalTypeAttribute)t.GetCustomAttribute(typeof(SingleRelationshipByFunctionalTypeAttribute))).Allowed,
                    LineageVersionsSupported = t.IsDefined(typeof(LineageVersionsSupportedAttribute), false) ?
                                                    ((LineageVersionsSupportedAttribute)t.GetCustomAttribute(typeof(LineageVersionsSupportedAttribute))).Versions :
                                                    new int[4] { 0, 1, 2, 3 }, //0 accounts for unit tests
                    SubjectAssetClassesSupported = ((SubjectAssetClassesSupportedAttribute)t.GetCustomAttribute(typeof(SubjectAssetClassesSupportedAttribute))).Classes,
                    ObjectAssetClassesSupported = ((ObjectAssetClassesSupportedAttribute)t.GetCustomAttribute(typeof(ObjectAssetClassesSupportedAttribute))).Classes
                };
        }
    }
}
