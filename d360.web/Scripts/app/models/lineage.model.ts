import { AssetTypeClass } from "./asset.model";
import { SelectItem, TreeNode } from "primeng/api";
import { ScoreType } from "./metrics.model";
import { PredicateType } from "./predicate.model";

//#region Enumerations

export enum DiagramObjectType {
    Link,
    Node
}

export enum LineageView {
    MapItemList = 0,
    MapRuleItemList = 4,
    SystemFlow = 1,
    DataFlow = 2,
    Technical = 3
}

export enum LineageEditorMode {
    Default,
    Preview,
    Summary
}

//#endregion

//#region Legacy: V1

export class LinkModel {
    id: number = null;
    key = null;
    Category: string = '';
    from = null;
    fromIntersectId: number = 0;
    fromPortId: string = 'OUT';
    to = null;
    toIntersectId: number = 0;
    toPortId: string = 'IN';
    text = null;
    type = null;
    diagramObjectType: DiagramObjectType = DiagramObjectType.Link;
    sourceMappingCount: number = 0;
    hasMappingRules: boolean = false;
    mappingRuleCount: number = 0;
    transformation = null;
    hasTransformations: boolean = false;
    hasProperties: boolean = false;
    mapItems = null;
}

export class NodeModel {
    key = null;
    obj = null;
    objid = null;
    name = null;
    textpath = null;
    shortname = null;
    typeName = null;
    type = null;
    back = null;
    fore = null;
    highlightColor = null;
    diagramObjectType: DiagramObjectType = DiagramObjectType.Node;
    template: string = 'Artifact';
    intersectId = null;
    sourceRuleCount: number = 0;
    sourceMappingCount: number = 0;
    hasMappingRules: boolean = false;
    mappingRuleCount: number = 0;
    hasSourceRules: boolean = false;
    actionCount: number = 0;
    hasActions: boolean = false;
    transformationCount: number = 0;
    hasTransformations: boolean = false;
    mapItems = null;
    other = null;
}

export class MapItem {
    MapItemID;
    SourceType;
    SourceName;
    Source;
    SourceID;
    SourceFusion;
    SourceFusionAttribute;
    SourceFusionAttributeType;
    TargetType;
    TargetName;
    Target;
    TargetID;
    TargetFusion;
    TargetFusionAttribute;
    TargetFusionAttributeType;

    searchableSource: string;
    searchableTarget: string;
    searchablSourceFusion: string;
    searchableTargetFusion: string;
}

export class TechnicalRelation {
    Object;
    ObjectID;
    ObjectName;
    ObjectUrl;
    ObjectTypeName;
}

export class Responsibility {
    ResponsibilityID;
    AssigningItemType;
    AssigningItemID;
    AssigningItemName;
    AssigningItemUrl;
    ResponsibleObjectType;
    ResponsibleObjectID;
    ResponsibleObjectName;
    PrimaryOwnerResourceID;
    PrimaryOwnerResourceName;
    PrimaryOwnerResourceUrl;
    ObjectType;
    ObjectID;
    Role;
    ResponsibleObjectUrl;
}

export class SourceRule {
    Contexts: string;
    Description: string;
    Sequence: number;
    SubjectID: number;
    SubjectName: string;
    SubjectTypeName: string;
    SubjectUrl: string;
}

export class MapSequenceModel {
    Available: MapSequenceItem[] = [];
    Contexts: MapContext[] = [];
    Referenced: MapReferenceItem[] = [];
}

export class MapSequenceItem {
    ID: number;
    Source: string;
    SourceIntersectID: number;
    Target: string;
    TargetIntersectID: number;
    isDeleting = false;
}

export class MapContext {
    Category: string;
    ID: number;
    Checked: boolean;
    Name: string;
    Type: string;
}

export class MapReferenceItem {
    ID: number;
    MapItemID: number;
    Sequence: number;
    Description: string;
    Contexts: MapContext[] = [];
    TargetIntersectID: number;
}

export class RelationItem {
    ID: number;
    IntersectTypeID: number;
    Object: string;
    ObjectID: number;
    TypeName: string;
    Name: string;
    Url: string;
}

export class AutoCompleteItem {
    valueField: string;
    labelField: string;
    value: number;
    label: string;

    templateValue: string;

    data: any;
}

export class LineageEditorRow {
    sourcekey: string;
    targetkey: string;
    ID: number;

    FocalObject: string;
    FocalID: number;

    SourceIntersectID: number;
    SourceIntersectTypeID: number = 0;
    SourceIntersectTypeName: string = '\u200B';
    SourceSubjectTypeName: string = '';
    SourceSubjectTypeID: number = 0;
    SourceSubjectType: string = '';
    SourceSubjectName: string = '';
    SourceSubject: string = '';
    SourceSubjectID: number = 0;
    SourceSubjectIconBackColor: string;
    SourceSubjectIconForeColor: string;
    SourceObjectTypeName: string = '';
    SourceObjectTypeID: number = 0;
    SourceObjectType: string = '';
    SourceObjectName: string = '';
    SourceObject: string = '';
    SourceObjectID: number = 0;
    SourceObjectIconBackColor: string;
    SourceObjectIconForeColor: string;
    TargetIntersectID: number;
    TargetIntersectTypeID: number = 0;
    TargetIntersectTypeName: string = '';
    TargetSubjectTypeName: string = '';
    TargetSubjectTypeID: number = 0;
    TargetSubjectType: string = '';
    TargetSubjectName: string = '';
    TargetSubject: string = '';
    TargetSubjectID: number = 0;
    TargetSubjectIconBackColor: string;
    TargetSubjectIconForeColor: string;
    TargetObjectTypeName: string = '';
    TargetObjectTypeID: number = 0;
    TargetObjectType: string = '';
    TargetObjectName: string = '';
    TargetObject: string = '';
    TargetObjectID: number = 0;
    TargetObjectIconBackColor: string;
    TargetObjectIconForeColor: string;
    HasSourceRules: boolean;
    HasError: boolean = false;
    ErrorMessage: string = '';

    //workaround p-autoComplete bug where value = '' shows as [object Object]
    //setting to string by default fixes this
    //https://github.com/primefaces/primeng/issues/910

    selectedSourceRelationshipType: AutoCompleteItem | string;
    selectedTargetRelationshipType: AutoCompleteItem | string;
    selectedSourceSubject: AutoCompleteItem | string;
    selectedSourceObject: AutoCompleteItem | string;
    selectedTargetSubject: AutoCompleteItem | string;
    selectedTargetObject: AutoCompleteItem | string;

    isNew: boolean = false;
    isDeleting: boolean = false;
    isConnected = true;
    isDupe = false;

}

export class LineageEditorTechnicalRow {
    ID: number;
    MapItemID: number;
    SourceFusionAttributeID: number;
    SourceFusionAttributeName: string;
    TargetFusionAttributeID: number;
    TargetFusionAttributeName: string;

    selectedSourceFusionAttribute: AutoCompleteItem | string;
    selectedTargetFusionAttribute: AutoCompleteItem | string;
    selectedMapItem: LineageEditorRow;

    isNew: boolean = false;
    isDeleting: boolean = false;
    isConnected = true;
    isDupe = false;

    HasError: boolean = false;
    ErrorMessage: string = '';
}

export class LineageEditorModel {
    FocalID: number;
    Focal: string;

    Adds: LineageEditorRow[] = [];
    Deletes: LineageEditorRow[] = [];
    Existing: LineageEditorRow[] = [];
}

export class LineageEditorTechnicalModel {
    FocalID: number;
    Focal: string;

    Adds: LineageEditorTechnicalRow[] = [];
    Deletes: LineageEditorTechnicalRow[] = [];
    Existing: LineageEditorTechnicalRow[] = [];
}

export class LineagePreviewModel {
    BusinessModel: LineageEditorModel;
    TechnicalModel: LineageEditorTechnicalModel;
}

export class SourceRuleItem {
    Available: SourceRuleSource[] = [];
    ID: number;
    Name: string;
    Selected: SourceRuleSequence[] = [];
    TargetIntersectID: number;
}

export class SourceRuleSequence {
    Contexts: any[] = [];
    Description: string;
    ID: number;
    MapItemID: number;
    Sequence: number;
    SourceName: string;
    IsDeleting: boolean = false;
}

export class SourceRuleSource {
    MapItemID: number;
    Name: string;
    SourceIntersectID: number;
}

//#endregion

// #region ASSET/IMPACT BROWSER

export enum AssetBrowserApiHopDirection {
    None = 0,
    Forward = 1,
    Backward = 2,
    Both = 3
}

export class AssetBrowserApiHopAssetRequestModel {
    Uid: string;
    Key: string;
}

export class AssetBrowserGenericRelationModel {
    from: string;
    to: string;
}

export enum DiagramType {
    Lineage = 1,
    Impact = 2,
    Process = 3
}

export class DiagramTypesModel {
    initial: number;
    items: any[] = [];
}

// #region Asset Browser : Responses

// Relationship Models

export class AssetBrowserResponseModel {
    nodes: AssetBrowserTranslationNode[];
    links: AssetBrowserTranslationLink[];
    hierarchy: AssetBrowserTranslationHierarchy[];
    reveals: AssetBrowserRevealNode[];
    dataLimitReached: boolean = false;
}

export class AssetBrowserRevealNode {
    hierarchyKey: string;
    from: string;
    to: string;
    direction: AssetBrowserApiHopDirection
}

export class AssetBrowserTranslationOwnerCount {
    key: string;
    responsibilityType: string;
    responsibilityTypeId: number;
    users: number[];
    count: number;
    expanded: boolean;
    id: string;
}

export class AssetBrowserTranslationRelationCount { 
    key: string;
    predicate: string;
    predicateId: number;
    predicateUid: string;
    direction: AssetBrowserApiHopDirection;
    count: number;
    expanded: boolean;
    disabled: boolean = false; //Used by the AB to determine whether to disable the badge while loading data. Prevents double-click issue.
}

export class AssetBrowserTranslationHierarchy {
    hierarchyKey: string;
    backwardReveal: AssetBrowserApiHopDirection;
    forwardReveal: AssetBrowserApiHopDirection;
    owners: AssetBrowserTranslationOwnerCount[] = [];
    relations: AssetBrowserTranslationRelationCount[] = [];
    predictableId: string;
}

export class AssetBrowserTranslationChildLink {
    id: number;
    from: string;
    to: string;
}

export class AssetBrowserTranslationLink {
    from: string;
    to: string;
    text: string;
    back: string;
    predicateIds: number[];
    responsibilityTypeId: number;
    predicateId: number;
    predicateUid: string;
    predicateType: PredicateType

    links: AssetBrowserTranslationChildLink[] = [];

    badgeIdentifier: string; 
}

export class AssetBrowserTranslationLinkIdentifier {
    predicateId: number;
    intersectUid: string;
}

export class AssetBrowserTranslationNode {
    hierarchyKey: string;
    hop: number;
    assetUid: string;
    assetTypeUid: string;
    assetTypeId: number;
    responsibilityTypeId: number;
    key: string;
    group: string;
    isGroup: boolean;
    text: string;
    template: string;
    nonHiddenTemplate: string;
    fore: string;
    foreAmount: number;
    back: string;
    backAmount: number;
    icon: string;
    class: AssetTypeClass;
    hasAssetReadAccess: boolean;
    showIcon: boolean;
    showReveal: AssetBrowserApiHopDirection;
    actionCount: number;
    owners: AssetBrowserTranslationOwnerCount[] = [];
    relations: AssetBrowserTranslationRelationCount[] = [];
    useAsTransformation: boolean;
    isSubjectInTransformation: boolean;

    leaf: boolean;
    focal: boolean;

    hideMode: AssetBrowserApiHopDirection = null;
    filterHiddenBy: string = null;

    predictableId: string;
}

// Ownership Models

export class DiagramOwnerCount {
    predictableId: string;
    owners: AssetBrowserTranslationOwnerCount[];
}

export class AssetBrowserOwnerRelationModel {
    assetUid: string;
    assetKey: string;
    ownerUid: string;
    ownerKey: string;
    backColor: string;
    foreColor: string;
}

export class AssetBrowserOwnersModel {
    owners: AssetBrowserOwnerModel[] = new Array<AssetBrowserOwnerModel>();
    ownerRelations: AssetBrowserOwnerRelationModel[] = new Array<AssetBrowserOwnerRelationModel>();
}

export class AssetBrowserOwnerModel {
    key: string;
    resourceUid: string;
    displayValue: string;
    icon: string;
    backColor: string;
    foreColor: string;
}

// #endregion Responses

//#region Asset Browser : AlertPanel Data

export class AssetBrowserAlertRequest {
    assets: AssetBrowserAlertAssetRequest[] = new Array<AssetBrowserAlertAssetRequest>();
}

export class AssetBrowserAlertAssetRequest {
    uid: string;
}

export class AssetBrowserAlert {
    uid: string;
    asset: AssetBrowserAlertAsset;
    action: AssetBrowserAlertAction;
    score: AssetBrowserAlertScore;
    selected: boolean = false;
}

export class AssetBrowserAlertAction {
    name: string;
    description: string;
}
export class AssetBrowserAlertAsset {
    uid: string;
    icon: string;
    displayValue: string;
}
export class AssetBrowserAlertScore {
    type: ScoreType;
    name: string;
    value: number;
    backColor: string;
}

//#endregion

//#region Asset Browser : InfoPanel Data

export class AssetBrowserDiagramAsset {
    AssetTypeClass: AssetTypeClass;
    AssetTypeClassDisplayName: string;
    TypeName: string;
    Uid: string;
    DisplayValue: string;
    Path: string;
    Url: string;
    Fields: AssetBrowserDiagramAssetField[] = [];
    Owners: AssetBrowserDiagramAssetOwner[] = [];
    Scores: AssetBrowserDiagramAssetScore[] = [];

    Loaded: boolean = false;
}

export class AssetBrowserDiagramAssetField {
    Name: string;
    Value: string;
    Type: string;
}

export class AssetBrowserDiagramAssetScore {
    Name: string;
    Value: number;
    LowerThreshold: number;
    UpperThreshold: number;
}

export class AssetBrowserDiagramAssetOwner {
    ResponsibilityTypeID: number;
    ResponsibilityTypeName: string;
    ResourceID: number;
    ResourceName: string;
}

//#endregion

//#region Asset Browser : FilterPanel Data

export enum FilterAncestryMode {
    AllAncestors = 1,
    DirectAncestor = 2,
    NoAncestor = 3
}

export class FilterAncestryOption {
    Mode: FilterAncestryMode;
    Text: string;
}

export class AssetBrowserAssetTypeFilterModel {
    Uid: string;
    Path: string;
    AssetTypeId: number;
    ClassId: number;
    Class: string;
}

export class AssetBrowserPredicateFilterModel {
    Id: number;
    Uid: number;
    Name: string;
    Inverse: string;
    TypeId: number;
    Type: string;
}

export class AssetBrowserResponsibilityTypeFilterModel {
    Id: number;
    Uid: number;
    Name: string;
}

export class AssetBrowserFilterModel {
    DiagramType: DiagramType = DiagramType.Lineage;
    AncestryMode: FilterAncestryMode = FilterAncestryMode.AllAncestors;
    DisplayBadges: boolean = true;
    DisplayIcons: boolean = true;
    DisplayScores: boolean = true;
    IncludeNonLeaf: boolean = true;
    NumberOfImpactHops: number = 1;
    NumberOfLineageHops: number = 3;
    SelectedAssetTypes: number[] = [];
    SelectedPredicates: number[] = [];
    SelectedResponsibilityTypes: number[] = [];
}

export enum AssetBrowserFilterChangeEventType {
    AssetType = 1,
    Predicate = 2,
    ResponsibilityType = 3,
    ImpactHopCount = 4,
    Ancestry = 5,
    AllBadges = 6,
    AncestorBadges = 7,
    Icons = 8,
    Scores = 9,
    DiagramType = 10,
    LineageHopCount = 11,
}

export class AssetBrowserFilterChangeEvent {
    Type: AssetBrowserFilterChangeEventType;
    Model: AssetBrowserFilterModel;
}

export class AssetBrowserPanelModel {
    selectedCommand: AssetBrowserPanelCommand;
    AddVisible: boolean = false;
    AlertVisible: boolean = false;
    FiltersVisible: boolean = false;
    InformationVisible: boolean = false;
    SettingsVisible: boolean = false;
}
export enum AssetBrowserPanelCommand {
    None = 0,
    Add = 1,
    Alerts = 2,
    Download = 3,
    Information = 4,
    Filters = 5,
    FullScreen = 6,
    Refresh = 7,
    Settings = 8
}

export class FilterSelectionsModel {
    AssetTypeOptions: AssetBrowserAssetTypeFilterModel[];
    PredicateOptions: AssetBrowserPredicateFilterModel[];
    ResponsibilityTypeOptions: AssetBrowserResponsibilityTypeFilterModel[];

    AncestryOptions: SelectItem[] = [
        { value: FilterAncestryMode.AllAncestors, label: 'Show all parents/owners' },
        { value: FilterAncestryMode.DirectAncestor, label: 'Show direct parent/owner' }
    ];

    HopOptions: SelectItem[] = [
        { label: 'One', value: 1 },
        { label: 'Two', value: 2 },
        { label: 'Three', value: 3 },
        { label: 'Four', value: 4 },
        { label: 'Five', value: 5 }
    ];

    FilterAssetTypes: TreeNode[] = [];
    FilterPredicates: TreeNode[] = [];
    FilterResponsibilityTypes: TreeNode[] = [];

    constructor(assetTypes: AssetBrowserAssetTypeFilterModel[], predicates: AssetBrowserPredicateFilterModel[], responsibilityTypes: AssetBrowserResponsibilityTypeFilterModel[]) {
        this.AssetTypeOptions = assetTypes;
        this.PredicateOptions = predicates;
        this.ResponsibilityTypeOptions = responsibilityTypes;
    }
}

export class LoadedFilterTypesModel {
    AssetTypes: number[] = [];
    Predicates: number[] = [];
    ResponsibilityTypes: number[] = [];
}

export class StoredAssetBrowserAssetTypeFilterModel {
    uid: string;
    class: string;
}

export class StoredAssetBrowserPredicateFilterModel {
    uid: number;
    type: string;
}

export class StoredAssetBrowserResponsibilityTypeFilterModel {
    uid: number;
    type: string;
}

export class StoredAssetBrowserFilterModel {
    uid: string;
    name: string;
    assetTypes: StoredAssetBrowserAssetTypeFilterModel[] = [];
    predicates: StoredAssetBrowserPredicateFilterModel[] = [];
    responsibilityTypes: StoredAssetBrowserResponsibilityTypeFilterModel[] = [];
    ancestryMode: number;
    numberOfHops: number;
    diagramType: number;
    isDefault: boolean;
    createdOn: string;
    updatedOn: string;
}

//#endregion

// #endregion
