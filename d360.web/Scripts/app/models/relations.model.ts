export class HierarchyModel {
    ID: number;
    Subject: string;
    Object: string;
    SubjectID: number;
    ObjectID: number;
    ObjectType: string;
    ObjectTypeID: number;
    ParentID: string;
    Name: string;
    Path: string;
    Url: string;
    ObjectTypeName: string;
    Level: number;
    PredicateID: number;
    PredicatePhrase: string;
    Type: PredicateType;
    GroupNumber: number;
    UID: string; 
}

export enum PredicateType {
    Lineage = 1,
    SourceToTarget = 2,
    TypeHierarchy = 3,
    GroupHierarchy = 4,
    ParentChildHierarchy = 5,
    Synonym = 6,
    Simple = 7
}