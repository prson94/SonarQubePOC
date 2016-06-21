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
    UpdatedOn: string;
    UpdatedBy: number;
}