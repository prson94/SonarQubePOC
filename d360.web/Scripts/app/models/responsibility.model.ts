
import { SelectItem } from '../models/form.model';

export interface IResponsibilityService {
    getResponsibilityDetail(assetID: number): Promise<ResponsibilityItemDetail[]>;
    getResponsibilityItemEditor(assetID: number, responsibilityID: number): Promise<ResponsibilityEditorModel>;
    postResponsibility(responsibility: ResponsibilityItem): Promise<any>;
}

export class ResponsibilityEditorModel {
    resources: SelectItem[];
    selectedResource: string;
    responsibilityTypes: SelectItem[];
    selectedResponsibilityType: string;
    responsibility: ResponsibilityItem;
    responsibilityDetails: ResponsibilityItemDetail[];
}

export class ResponsibilityItem {
    ID: number;
    ResponsibilityTypeID: number;
    AssetID: number;
    SecurityAsset: string;
    SecurityAssetID: number;
    Context: string;
}

export class ResponsibilityItemDetail {
    AssetID: number;
    AssetTypeID: number;

    OverrideID: number;

    RuleID: number;

    ResponsibilityTypeID: number;
    ResponsibilityTypeName: string;

    ResourceName: string;
    ResourceID: number;

    SecurityAsset: string;
    SecurityAssetID: number;
    SecurityAssetName: string;

    Context: string;

    ApplyToType: boolean;
    PermissionsBitMask: number;
    IsVisible: boolean;

    Object: string;
    ObjectID: number;
    Type: string;
    TypeID: number;
}