export class RelationshipTypePredicate {
    Uid: string;
    Type: string;
    Name: string;
    Inverse: string;
}
export class RelationshipTypeEdge {
    Uid: string;
    Name: string;
    Class: string;
    Cardinality: string;
}
export class RelationshipType {
    Id: number;
    Uid: string;
    State: string;
    IsSystem: boolean;
    Predicate: RelationshipTypePredicate;
    Subject: RelationshipTypeEdge;
    Object: RelationshipTypeEdge;
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
    uid: number;
    Cardinality: number;
    AllowEditFromRelationshipEditor: boolean;
}


export class PossibleTechnicalRelationship {
    Title: string;
    IntersectTypeID: number;
    ObjectType: string;
    ParentIntersectTypeID: number;
}
