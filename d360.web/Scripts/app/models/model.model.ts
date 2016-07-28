export class Model {
    Name: string;
    Description: string;
    MaximumDepth: number;
    TaxonomyTypeClass: string;
    ID: number;
}

export class ModelHierarchy {
    HasChildren: boolean;
    ID: number;
    Name: string;
    ParentID: number;
}