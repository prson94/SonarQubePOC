import { AssetTypeClass } from "./asset.model";
import { SelectItem, TreeNode } from "primeng/api";
import { ScoreType } from "./metrics.model";
import { PredicateType } from "./predicate.model";

export enum DiagramObjectType {
    Link,
    Node
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
    id: number;
    text: string;
    count: number;
    key: string;
    users: number[];
    expanded: boolean;
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

    childCount: number;
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
    Url: string;
    Id: number;
    Object: string;
    ObjectId: number;
    Scores: AssetBrowserDiagramAssetScore[] = [];
    Fields: any[] = [];
    Loaded: boolean = false;
    AssetTypeUid: string;
}

export class AssetBrowserDiagramAssetField {
    Name: string;
    Value: string;
    Type: string;
}

export class AssetBrowserDiagramAssetScore {
    Name: string;
    Value: number;
    ScoreClass: string;
    LowerThreshold: number;
    UpperThreshold: number;
}

export class AssetBrowserDiagramAssetOwner {
    ResponsibilityTypeUid: string;
    ResponsibilityTypeName: string;
    ResourceUid: string;
    ResourceName: string;
}

//#endregion

//#region Asset Browser : FilterPanel Data

export enum FilterAncestryMode {
    AllAncestors = 1,
    DirectAncestor = 2,
    NoAncestor = 3
}

export enum FilterDescendancyMode {
    None = 1,
    Direct = 2,
    All = 3
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
    DisplayDescendantAssets: boolean = true;
    Descendancy: FilterDescendancyMode = FilterDescendancyMode.None;
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
    Descendancy = 12 
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
        { value: FilterAncestryMode.AllAncestors, label: $localize`Show all parents/owners` },
        { value: FilterAncestryMode.DirectAncestor, label: $localize`Show direct parent/owner` }
    ];

    DescendancyOptions: SelectItem[] = [
        { value: FilterDescendancyMode.None, label: $localize`None` },
        { value: FilterDescendancyMode.Direct, label: $localize`Direct children only` },
        { value: FilterDescendancyMode.All, label: $localize`All descendants` }
    ];

    HopOptions: SelectItem[] = [
        { label: $localize`One`, value: 1 },
        { label: $localize`Two`, value: 2 },
        { label: $localize`Three`, value: 3 },
        { label: $localize`Four`, value: 4 },
        { label: $localize`Five`, value: 5 }
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

export class AssetBrowserLineageRequest {
    ancestry: FilterAncestryMode;
    descendancy: FilterDescendancyMode;
    currentHop: number;
    direction: AssetBrowserApiHopDirection;
    includeNonLeaf: boolean;
    assets: AssetBrowserApiHopAssetRequestModel[];
    intersects: number[];
    hierarchyKey: string;
}

// #endregion
