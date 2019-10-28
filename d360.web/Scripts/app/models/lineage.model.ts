import { AssetTypeClass } from "./asset.model";

export class LineageNode {
    key: any;
    assetId: any;
    assetTypeId: any;
    object: string;
    objectId: number;
    objectTypeName: string;
    objectType: string;
    objectTypeId: number;

    name: string;
    foreColor: string = '#000';
    backColor: string = '#fff';
    visible: boolean = true;

    get isNew(): boolean {
        return isNaN(+this.key) ? false : +this.key >= 0;
    }

    hiddenNodeKey: any;
    category: string;
    diagramObjectType: DiagramObjectType = DiagramObjectType.Node;
    template: any = null;

    valid: boolean = true;
    errors = [];
}

export class LineageLink {
    from: string;
    to: string;
    intersectId: number = -1;
    intersectTypeId: number = -1;
    state: number;
    predicate: string;
    predicates: PredicateInfo[] = [];

    get text() {
        return this.getText(22);
    }

    get fullText() {
        return this.getText();
    }

    get isNew(): boolean {
        return (isNaN(+this.from) ? false : +this.from >= 0) || (isNaN(+this.to) ? false : +this.to >= 0)
    }
    private getText(len: number = Infinity) {
        let name = "";
        if (this.predicates == null || this.predicates.length < 1)
            name = this.predicate || "";
        else {
            this.predicates.forEach(p => {
                name += p.name + ', ';
            });
            //remove trailing ,
            name = name.substr(0, name.length - 2);
        }

        if (name != null && name.length > len)
            name = name.substr(0, len) + '...';

        return name;
    }

    valid: boolean = true;
    errors = [];

    category: string;
    diagramObjectType: DiagramObjectType = DiagramObjectType.Link;
}

export class PredicateInfo {
    intersectTypeId: number;
    name: string;
    intersectId: number;
}

export class LineageEditorModelV2 {
    Object: string;
    ObjectID: number;
    Nodes: LineageNode[] = [];
    Links: LineageLink[] = [];
    OriginalNodes: LineageNode[] = [];
    OriginalLinks: LineageLink[] = [];
}

export class AssetTypeFilter {
    id: number;
    name: string;
    selected: boolean = true;
}

export class SidebarView {
    name: string;
    tabs: string[] = [];
    currentTab: string;
}


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

//#region Legacy

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

// #region Asset Browser : Translation

export class AssetBrowserTranslationRelationCount {
    key: string;
    predicate: string;
    predicateUid: string;
    direction: AssetBrowserDirection;
    count: number;
}

export class AssetBrowserTranslationLink {
    from: string;
    fromPort: string;
    to: string;
    toPort: string;
    text: string;
    back: string;
    impacts: string[];
}

export class AssetBrowserTranslationNode {
    hop: number;
    assetUid: string;
    key: string;
    group: string;
    isGroup: boolean;
    text: string;
    template: string;
    back: string;
    icon: string;
    subgraph: any;
    showReveal: AssetBrowserDirection;
    relations: AssetBrowserTranslationRelationCount[] = new Array();
    currentRelations: AssetBrowserTranslationRelationCount[] = new Array();
}

export class AssetBrowserTranslation {
    links: AssetBrowserTranslationLink[] = new Array();
    nodes: AssetBrowserTranslationNode[] = new Array();
}

// #endregion Translation

// #region Asset Browser : Request

export enum AssetBrowserDirection {
    None = 0,
    Forward = 1,
    Backward = 2,
    Both = 3
}

export class AssetBrowserLineageApiRequestModel {
    AssetUids: string[];
    IsReveal: boolean;
    StartHop: number;
    Direction: AssetBrowserDirection;
    Hops: number;
}

export class AssetBrowserImpactApiRequestModel {
    Assets: AssetBrowserImpactApiAssetRequestModel[];
    PredicateUid: string;
    StartHop: number;
}

export class AssetBrowserImpactApiAssetRequestModel {
    Uid: string;
    Key: string;
}

// #endregion Request

// #region Asset Browser : Response

export class AssetBrowserLineageApiItemRelationCountModel {
    Predicate: string;
    PredicateUid: string;
    Direction: AssetBrowserDirection;
    Count: number;
}

export class AssetBrowserLineageApiItemModel {
    hop: number;
    assetUid: string;
    key: string;
    displayValue: string;
    backColor: string;
    foreColor: string;
    reveal: AssetBrowserDirection;
    items: AssetBrowserLineageApiItemModel[];
    relationCounts: AssetBrowserLineageApiItemRelationCountModel[];
}

export class AssetBrowserLineageApiRelationshipModel {
    intersectUid: string;
    subjectUid: string;
    subjectKey: string;
    objectUid: string;
    objectKey: string;
    predicate: string;
    predicateUid: string;
    predicateType: number;
    backColor: string;
    foreColor: string;
}

export class AssetBrowserLineageApiResponseModel {
    focalAssetUid: string;
    assets: AssetBrowserLineageApiItemModel[];
    intersects: AssetBrowserLineageApiRelationshipModel[];
}

// #endregion Response

//#region Asset Browser : InfoPanel Data

export class AssetBrowserDiagramAsset {
    AssetTypeClass: AssetTypeClass;
    AssetTypeClassDisplayName: string;
    TypeName: string;
    Uid: string;
    DisplayValue: string;
    Path: string;
    Url: string;
    Fields: AssetBrowserDiagramAssetField[];
    Owners: AssetBrowserDiagramAssetOwner[];
    Scores: AssetBrowserDiagramAssetScore[];
}

export class AssetBrowserDiagramAssetField {
    Name: string;
    Value: string;
    Type: string;
}

export class AssetBrowserDiagramAssetScore {
    Name: string;
    Value: number;
}

export class AssetBrowserDiagramAssetOwner {
    ResponsibilityTypeID: number;
    ResponsibilityTypeName: string;
    Icon: string;
    ResourceID: number;
    ResourceName: string;
    SecurityAssetName: string;
    Context: string;
}

//#endregion