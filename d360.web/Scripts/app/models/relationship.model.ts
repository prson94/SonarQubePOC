export class RelationshipType {
    ID: number;
    Uid: string;
    IsSystem: boolean;
    Object: string;
    ObjectID: number;
    ObjectUid: string;
    ObjectTypeName: string;
    PredicateID: number;
    PredicateName: string;
    PredicateInverse: string;
    Subject: string;
    SubjectID: number;
    SubjectUid: string;
    SubjectTypeName: string;
}

export class RelationshipDetail {
    ID: number;
    LimitedChangesOnly: boolean;
    Predicate: number;
    Subject: string;
    SubjectDisplayText: string;
    SubjectCardinality: number;
    Object: string;
    ObjectDisplayText: string;
    ObjectCardinality: number;
}

export class ObjectRelationship {
    IntersectTypeID: number;
    ParentIntersectID: number;
    TargetName: string;
    TargetType: string;
    TargetTypeID: number;
    PredicateName: string;
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
    Cardinality: number;
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