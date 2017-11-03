
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
}

export class ResponsibilityItem {
    ID: number;
    ResponsibilityTypeID: number;
    AssetID: number;
    SecurityAsset: string;
    SecurityAssetID: number;
}

export class ResponsibilityItemDetail {
    AssetID: number;
    Object: string;
    ObjectID: number;
    OverrideItemID: number;
    Type: string;
    TypeID: number;
    RuleName: string;
    ResponsibilityTypeID: number;
    ResponsibilityTypeName: string;
    FirstName: string;
    LastName: string;
    ResourceID: number;
    SecurityAsset: string;
    SecurityAssetID: number;
    SecurityAssetName: string;
}