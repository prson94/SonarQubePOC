export class ImpactDiagramModel {
    links: LinkModel[] = [];
    nodes: NodeModel[] = [];
}

export class LinkModel {
    from: string;
    intersectid: number;
    text: string;
    predicateid: number;
    to: string;
    visible: boolean = true;
    category: string;
    isTreeLink: boolean = true;
}

export class NodeModel {
    back: string;
    fore: string;
    intersectid: number;
    key: string;
    name: string;
    obj: string;
    objid: number;
    predicate: string;
    predicateid: number;
    typeName: string;
    typeNamePlural: string;
    type: string;
    typeId: number;
    everExpanded: boolean = false;
    isTreeExpanded: boolean;
    category: string;
    visible: boolean = true;
    childCount: number;
}

export class ImpactFilter {
    key: string;
    name: string;
    type: FilterType;
    selected: boolean = true;
}

export enum FilterType {
    Predicate,
    Category
}

