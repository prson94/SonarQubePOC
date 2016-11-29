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
    type: string;
    typeId: number;
    everExpanded: boolean = false;
    isTreeExpanded: boolean;
    category: string;
    visible: boolean = true;
    childNodes: NodeModel[] = [];
}

export class PredicateFilter {
    id: number;
    name: string;
    selected: boolean = true;
}