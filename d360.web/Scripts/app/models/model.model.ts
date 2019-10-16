import { NymType } from './object-detail.model';

export class Model {
    Name: string;
    Description: string;
    MaximumDepth: number;
    TaxonomyTypeClass: string;    
    HasDashboards: boolean;
    AllowAttributes: boolean;
    AllowSynonyms: boolean;
    ID: number;
    NymTypes: NymType[];
    P_CanDelete: boolean;
    P_CanEdit: boolean;
    AssetTypeUID: string;
}

export class ModelHierarchy {
    HasChildren: boolean;   
    ID: number;
    Uid: string;
    AssetID: number;
    DisplayValue: string;
    TextPath: string;
    ParentID: number;
    Level: number;
    HasDashboards: boolean;
    HasWorkflow: boolean;
}

export class HierarchyDiagramModel {
    assetId: number;
    RelationshipsExist: boolean;
    children: HierarchyDiagramModel[] = [];
    key: string;
    name: string;
    parent: string;
    url: string;

}