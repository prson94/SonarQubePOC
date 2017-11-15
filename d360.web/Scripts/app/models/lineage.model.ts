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



export class NodeModelV2 {
    key: any;
    object: string;
    objectId: number;
    objectType: string;
    objectTypeId: number;
    objectTypeName: string;
    name: string;
    foreColor: string = '#000';
    backColor: string = '#fff';
    visible: boolean = true;
    order: number;
    intersectTypeId: number;

    businessTransformation: string;
    technicalTransformation: string;

    group: string;
    isGroup: boolean = false;
    category: string;
    diagramObjectType: DiagramObjectType = DiagramObjectType.Node;
    template: any = null;
    templateId: number;
    isRequired: boolean = null;

    level: number = -1;
    itemKey: string;
    fromItemKey: string;
    graphKey: string;

    valid: boolean = true;
    errors = [];
}

export class LinkModelV2 {
    from: string;
    to: string;
    intersectId: number;

    category: string;
    diagramObjectType: DiagramObjectType = DiagramObjectType.Link;
}


export class LineageEditorModelV2 {
    Focal: string;
    FocalID: number;
    Nodes: LineageNodeModel[] = [];
    Links: LineageLinkModel[] = [];
}

export class LineageNodeModel {
    Key: string;
    Object: string;
    ObjectID: number;
    ObjectType: string;
    ObjectTypeID: number;
    Group: string;
    IsGroup: boolean;
    Category: string;
    BusinessTransformation: string;
    TechnicalTransformation: string;
    Order: number;
    IntersectTypeID: number;
    MapTypeTemplateID: number;
}

export class LineageLinkModel {
    IntersectID: number;
    From: string;
    To: string;
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

export class TechnicalRelation {
    Object;
    ObjectID;
    ObjectName;
    ObjectUrl;
    ObjectTypeName;
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

export class IntersectDetail {
    ID: number;
    IntersectTypeID: number;
    Classification: number;
    Description: string;
    Subject: string;
    SubjectID: number;
    SubjectName: string;
    SubjectUrl: string;
    SubjectType: string;
    SubjectTypeID: number;
    SubjectTypeName: string;
    SubjectIconBackColor: string;
    SubjectIconForeColor: string;
    SubjectIconText: string;
    Object: string;
    ObjectID: number;
    ObjectName: string;
    ObjectUrl: string;
    ObjectType: string;
    ObjectTypeID: number;
    ObjectTypeName: string;
    ObjectIconBackColor: string;
    ObjectIconForeColor: string;
    ObjectIconText: string;
    PredicateID: number;
    PredicateName: string;
    PredicateType: number;
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

//#region enumerations

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


