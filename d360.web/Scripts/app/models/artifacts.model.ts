import { BreadcrumbItem } from './breadcrumb.model';

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
    Description: string;
    ID: number;
    Name: string;
    Status: string;
    TypeName: string;
    Breadcrumbs: BreadcrumbItem[];
}