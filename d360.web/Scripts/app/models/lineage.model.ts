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
    challengeCount: number = 0;
    hasChallenges: boolean = false;
    openEventCount: number = 0;
    hasOpenEvents: boolean = false;
    openIssueCount: number = 0;
    hasOpenIssues: boolean = false;
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

export enum DiagramObjectType {
    Link,
    Node
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