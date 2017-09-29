import { NymType } from './object-detail.model';

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
    NymTypes: NymType[];
}

export class ModelHierarchy {
    HasChildren: boolean;    
    ID: number;
    DisplayValue: string;
    TextPath: string;
    ParentID: number;
    Level: number;
}

export class HierarchyDiagramModel {
    RelationshipsExist: boolean;
    children: HierarchyDiagramModel[] = [];
    key: string;
    name: string;
    parent: string;
    url: string;

}