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

export class ObjectRelationship {
    IntersectTypeID: number;
    ParentIntersectID: number;
    TargetName: string;
    TargetType: string;
    TargetTypeID: number;
}

export class RelatedItem {
    Name: string;
    Type: string;
    ID: number;
}

export class ObjectRelationshipCount {
    Object: string;
    ObjectId: number;
    Name: string;
    Count: number;
}
