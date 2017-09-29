import { BaseEditorModel } from '../models/form.model';

export class ArtifactTypeEditorModel extends BaseEditorModel {
    IconBackColor: string;
    IconForeColor: string;
    ArtifactType: ArtifactType;
}

export class ArtifactType {
    ID: number;
    ParentID: number;
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
}

export class ArtifactTypeExportTemplate {
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