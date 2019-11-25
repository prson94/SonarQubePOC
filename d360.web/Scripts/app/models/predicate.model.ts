export class Predicate {
    Name: string;
    Inverse: string;
    Type: string;
    IsInUse: boolean;
    IsSystem: boolean;
    Uid: number;
}

export enum PredicateType {
    Simple = 'Simple',
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
    BusinessToTechnical ='BusinessToTechnical'
}