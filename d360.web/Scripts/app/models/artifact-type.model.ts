import { BaseEditorModel } from '../models/form.model';
import { AssetTypeClass } from './asset.model';


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
    Class: AssetTypeClass;
}

export class AssetTypeExportTemplate {
    ID: number;
    Name: string;
    Description: string;
}
