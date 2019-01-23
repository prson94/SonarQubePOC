import { BaseEditorModel } from '../models/form.model';
import { Predicate } from "./predicate.model";

export class ArtifactTypeEditorModel extends BaseEditorModel {
    IconBackColor: string;
    IconForeColor: string;
    SelectedPredicateID: number;
    ParentID: number;
    Predicates: any[];
    Tokens: any[];
    ArtifactType: ArtifactType;
}

export class ArtifactType {
    ID: number;
    ParentID: number;
    AssetTypeID: number;
    Name: string;
    DisplayFormat: string;
    Description: string;
    AllowHierarchy: boolean;
    AllowRelatedArtifacts: boolean;
    AutoDisplayDescription: boolean;
    CanOwnFusion: boolean;
    HasDashboards: boolean;    
    HasV2Workflows: boolean;
    HasCustomExportTemplates: boolean;
    AssetTypeUID: string;
}

export class AssetTypeExportTemplate {
    ID: number;
    Name: string;
    Description: string;
}

export class ArtifactTypeSummary {
    ID: number;
    Description: string;
    Name: string;
    ParentID: number;
    Total: number;
    expanded: boolean;
}

export class ArtifactTypeStatusCount {
    Status: string;
    Count: number;
    BackColor: string;
}

export class ArtifactTypeUsedVsUnusedResponsibility {
    ArtifactType: string;
    ArtifactTypeID: number;
    AssignedCount: number;
    Responsibility: string;
    Total: number;
    UnassignedCount: number;
}