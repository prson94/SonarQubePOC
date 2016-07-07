export class Relationship {
    ID: number;
    Source: string;
    SourceID: number;
    SourceName: string;
    Target: string;
    TargetID: number;
    TargetName: string;
}

export class RelationshipDetail {
    ID: number;
    LimitedChangesOnly: boolean;
    Predicates: number[];
    Side1: string;
    Side1DisplayText: string;
    Side2: string;
    Side2DisplayText: string;
}
