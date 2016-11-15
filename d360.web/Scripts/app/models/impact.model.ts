export class ImpactDiagramModel {
    links: LinkModel[] = [];
    nodes: NodeModel[] = [];
}

export class LinkModel {
    from: string;
    intersectid: number;
    text: string;
    to: string;
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
    typeName: string;
    everExpanded: boolean = false;
    template: string;
}