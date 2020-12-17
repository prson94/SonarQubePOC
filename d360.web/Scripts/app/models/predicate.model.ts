export class Predicate {
    Name: string;
    Inverse: string;
    Type: string;
    FriendlyTypeName: string;
    IsInUse: boolean;
    IsSystem: boolean;
    Uid: string;
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
    SemanticRelation = 'SemanticRelation',
    Diagram = 'Diagram',
    DiagramUse = 'DiagramUse',
    DiagramReference = 'DiagramReference'
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
    SemanticRelation = 'Semantic Relation',
    Diagram = 'Diagram',
    DiagramUse = 'Diagram Use',
    DiagramReference = 'Diagram Reference'
}