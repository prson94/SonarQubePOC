import { BreadcrumbItem } from './breadcrumb.model';
import { NymType } from './object-detail.model';

export class Artifacts {
    results: any[];
    total: number;
}


export class Artifact {
    AllowAttributes: boolean;
    AllowPredicateHierarchies: boolean;
    AllowRelatedArtifacts: boolean;
    AllowSynonyms: boolean;
    ArtifactTypeID: number;
    HasDashboards: boolean;
    HasWorkflow: boolean;
    HasChildArtifacts: boolean;
    Uid: string;
    ID: number;
    AssetID: number;
    AssetTypeID: number;
    DisplayValue: string;
    TypeName: string;
    Breadcrumbs: BreadcrumbItem[];
    NymTypes: NymType[];
}