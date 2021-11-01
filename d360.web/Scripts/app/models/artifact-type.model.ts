import { BaseEditorModel } from '../models/form.model';
import { AssetTypeClass } from './asset.model';
import { AssetGridObject } from '../components/assets-grid/asset-grid.model';


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
    HasDashboards: boolean;
    HasV2Workflows: boolean;
    HasCustomExportTemplates: boolean;
    AssetTypeUID: string;
    Class: AssetTypeClass;
    AutoDisplayParent: boolean;

    public static AsGridObject(artifact: ArtifactType): AssetGridObject {
        var ago = new AssetGridObject();
        ago.AssetTypeUID = artifact.AssetTypeUID;
        ago.AutoDisplayDescription = artifact.AutoDisplayDescription;
        ago.Description = artifact.Description;
        ago.HasCustomExportTemplates = artifact.HasCustomExportTemplates;
        ago.ID = artifact.ID;
        ago.Name = artifact.Name;
        ago.Object = 'Artifact';
        ago.ObjectType = 'ArtifactType';
        ago.AutoDisplayParent = artifact.AutoDisplayParent
        return ago;
    }

}

export class AssetTypeExportTemplate {
    Name: string;
    Description: string;
    Uid: string;
}
