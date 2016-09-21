export class Model {
    Name: string;
    Description: string;
    MaximumDepth: number;
    TaxonomyTypeClass: string;
    ClassificationName: string;
    HasDashboards: boolean;
    ID: number;
}

export class ModelHierarchy {
    HasChildren: boolean;    
    ID: number;
    Name: string;
    ParentID: number;
    Description: string;
}