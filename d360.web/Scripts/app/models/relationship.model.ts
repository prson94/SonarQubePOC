export class Relationship {
    ID: number;
    Object: string;
    ObjectID: number;
    ObjectName: string;
    PredicateID: number;
    PredicateName: string;
    Subject: string;
    SubjectID: number;
    SubjectName: string;
}

export class RelationshipDetail {
    ID: number;
    LimitedChangesOnly: boolean;
    Predicate: number;
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
    ObjectID: number;
    Name: string;
    Count: number;
    IntersectTypeID: number;
}


export class PossibleTechnicalRelationship {
    Title: string;
    IntersectTypeID: number;
    ObjectType: string;
    ParentIntersectTypeID: number;
}

export class RelationshipRole {
    ID: number;
    Name: string;
    Description: string;
    IsUsed: boolean;
}