import { Predicate } from "./predicate.model";

export enum Cardinality {
    One = 1,
    Many = 2
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
    Predicate: Predicate;
    Subject: RelationshipTypeEdge;
    Object: RelationshipTypeEdge;
}

export class RelationshipTypeApiRequestModel {
    Uid: string;

}


export class RelationshipCount {
    IntersectTypeUid: string;
    Count: number;
    IsSubject: boolean;
}

export class RelationshipTypeUIModel extends RelationshipType {
    Count: number;
    TypeName: string;
    AllowEditFromRelationshipEditor: boolean = true;
    IsSubject: boolean = false;
}

export class RelationshipDetail {
    ID: number;
    LimitedChangesOnly: boolean;
    Predicate: string;
    PredicateType: number;
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
    Uid: string;
}

export class RelatedItem {
    Name: string;
    Type: string;
    ID: number;
    Uid: string;
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
    IsSubject: boolean;
    ObjectUid: string;
}


export class PossibleTechnicalRelationship {
    Title: string;
    IntersectTypeID: number;
    ObjectType: string;
    ParentIntersectTypeID: number;
}

export class PredicateDropdown {
    label: string;
    value: string;
    isSemantic: boolean;
    type: string;
}

export class RelationshipV2 {
    SubjectAssetUid: string;
    ObjectAssetUid: string;
    Fields: any = {};
}