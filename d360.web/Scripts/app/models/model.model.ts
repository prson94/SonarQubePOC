export class Model {
    Name: string;
    Description: string;
    MaximumDepth: number;
    TaxonomyTypeClass: string;
    ClassificationName: string;
    HasDashboards: boolean;
    AllowAttributes: boolean;
    AllowSynonyms: boolean;
    ID: number;
}

export class ModelHierarchy {
    HasChildren: boolean;    
    ID: number;
    Name: string;
    ParentID: number;
    Description: string;
}

export class ModelClassification {
    ID: number;
    Name: string;
}