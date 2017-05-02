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
    Description: string;
    AllowHierarchy: boolean;
    AllowRelatedArtifacts: boolean;
    CanOwnFusion: boolean;
    HasDashboards: boolean;    
    HasV2Workflows: boolean;
}

export class ArtifactTypeSummary {
    ID: number;
    Certified: number;
    Description: string;
    Draft: number;
    Name: string;
    ParentID: number;
    Total: number;
    UnderReview: number;
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