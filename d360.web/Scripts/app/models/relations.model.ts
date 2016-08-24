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

export class HierarchyArtifactsModel {
    ID: number;
    IntersectMapID: number;
    MapType: PredicateType;
    Type: string;
    GroupNumber: number;
    IsAddingParent: boolean;
}

export class HierarchyArtifactItem {
    DisplayName: string;
    Name: string;
    Object: string;
    ObjectID: number;
    ObjectTypeName: string;
}


export class HierarchyPostModel {
    IntersectMapID: number;
    HierarchyType: PredicateType;
    PredicateID: number;
    IsAddingParent: boolean = false;
    ObjectID: number;
    Object: string;
    ObjectType: string;
    ObjectTypeID: number;
    SubjectID: number;
    Subject: string;
    SubjectType: string;
    SubjectTypeID: number;
    GroupNumber: number = -1;
}
