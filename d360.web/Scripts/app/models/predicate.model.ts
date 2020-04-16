export class Predicate {
    Name: string;
    Inverse: string;
    Type: string;
    FriendTypeName: string;
    IsInUse: boolean;
    IsSystem: boolean;
    Uid: number;
}

export enum PredicateType {
    Simple = 'Simple',
    Evaluation = 'Evaluation',
    DataLineage ='DataLineage',
    ReferenceLineage ='ReferenceLineage',
    InterTypeHierarchy = 'InterTypeHierarchy',
    IntraTypeHierarchy ='IntraTypeHierarchy',
    UserOwnership ='UserOwnership',
    Grammar ='Grammar',
    FusionMapping ='FusionMapping',
    SeeAlso ='SeeAlso',
    Usage ='Usage',
    ObjectOwnerhip ='ObjectOwnerhip',
    Transformation ='Transformation',
    BusinessToTechnical = 'BusinessToTechnical',
    SemanticRelation = 'SemanticRelation'
}

export enum PredicateFriendlyType {
    Simple = 'Simple',
    Evaluation = 'Evaluation',
    DataLineage = 'Simple Data Lineage',
    ReferenceLineage = 'Reference Data Lineage',
    InterTypeHierarchy = 'Inter-type Hierarchy',
    IntraTypeHierarchy = 'Intra-type Hierarchy',
    UserOwnership = 'User Ownership',// - NOT USED YET
    Grammar = 'Grammatic Association',
    FusionMapping = 'Mapping',
    SeeAlso = 'See Also',
    Usage = 'Usage',
    ObjectOwnerhip = 'Object Ownerhip',
    Transformation = 'Transformation',
    BusinessToTechnical = 'Business To Technical',
    SemanticRelation = 'Semantic Relation'
}