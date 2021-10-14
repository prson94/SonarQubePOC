import { BreadcrumbItem } from './breadcrumb.model';
import { NymType } from './object-detail.model';
import { AssetTypeClass } from './asset.model';

export class Artifacts {
    results: any[];
    total: number;
    items: any;
}

export class ArtifactPermission {
    addArtifact: boolean;
    editArtifact: boolean;
    deleteArtifact: boolean;
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
    Class: AssetTypeClass;
    Breadcrumbs: BreadcrumbItem[];
    NymTypes: NymType[];
    ArtifactPermission: ArtifactPermission
}