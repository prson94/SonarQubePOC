/*eslint camelcase: ["error", {properties: "never"}]*/

export class StringConstants {
    //object types
    static ObjectArtifact = "Artifact";
    static ObjectArtifactType = "ArtifactType";
    static ObjectRelationship = "Relationship";
    static ObjectGovernance = "Governance";
    static ObjectRoot = "Root";
    static ObjectTaxonomy = "Taxonomy";
    static ObjectRule = "Rule";
    static ObjectPolicy = "Policy";
    static ObjectResource = "Resource";
    static ObjectTaxonomyType = "TaxonomyType";
    static ObjectPolicyType = "PolicyType";
    static ObjectRuleType = "RuleType";
    static ObjectGroup = "Group";

    //claim types
    static ClaimRead = "Read";
    static ClaimDelete = "Delete";
    static ClaimCreate = "Create";
    static ClaimUpdate = "Update";

    static AssetTypeClass_Business = "Business Asset";
    static AssetTypeClass_Technical = "Technical Asset";

    static Area_Administration = $localize`Administration`;
    static Area_Configuration = $localize`Configuration`;
    static SubArea_Security = $localize`Security`;
    static Section_Actions = $localize`Workflow Actions`;
    static Section_Branding = $localize`Branding`;
    static Section_BusinessAssets = $localize`Business Assets`;
    static Section_CustomApi = $localize`Custom API`;
    static Section_Dashboards = $localize`Dashboards`;
    static Section_ExportTemplates = $localize`Export Templates`;
    static Section_Groups = $localize`Groups`;
    static Section_Bulk = $localize`Bulk Loader`;
    static Section_Artifacts = $localize`Artifacts`;
    static Section_Models = $localize`Models`;
    static Section_Organizations = $localize`Organizations`;
    static Section_Policies = $localize`Policies`;
    static Section_Predicates = $localize`Predicates`;
    static Section_Relationships = $localize`Relationships`;
    static Section_Responsibilities = $localize`Responsibilities`;
    static Section_Rules = $localize`Rules`;
    static Section_Scoring = $localize`Scoring Definitions`;
    static Section_Search = $localize`Search Index`;
    static Section_Settings = $localize`Settings`;
    static Section_Surveys = $localize`Surveys`;
    static Section_Tags = $localize`Tags`;
    static Section_TechnicalAssets = $localize`Technical Assets`;
    static Section_Users = $localize`Users`;
    static Section_Workflows = $localize`Workflows`;
    static Section_SemanticTypes = $localize`Semantic Types`;

    static MenuId_Favorites = "*Favorites";

    static simpleSearchTooltipHTML: string = $localize`<p>Type to provide a search term. Matches will be found where the value of any field starts with the term or terms provided.</p><p>You can also use wildcards for more control over how the term is matched.
*term* : Match on values which contain 'term'</p><p>All matches are case insensitive.</p>`;
}