
import { SelectItem } from '../models/form.model';
import { Observable } from 'rxjs';

export interface IResponsibilityService {    
    getResponsibilityDetail(assetUid: string): Observable<ResponsibilityItemDetailV2[]> 
    getResponsibilityItemEditor(assetID: number, responsibilityID: number, assetUid: string, responsibilityUid: string, resourceUid: string): Observable<ResponsibilityEditorModel>;
    postResponsibility(assetUid: string, responsibilityUid: string, responsibilityUids: any): Observable<any>;
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

export class ResponsibilityOverridePostModel {
    ResourceUid: string[];
    Description: string;
}

export class ResponsibilityOverrideDeleteModel {
    ResourceUid: string;
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

export class ResponsibilityItemDetailV2 {
    Responsibility: string;

    ResponsibilityUid: string;

    Resource: string;

    ResourceUid: string;

    GroupResourceUid: string;

    Description: string;

    Group: string;

    AssignedBy: string;

    IsVisible: boolean;

    ResourceType: string;
}

export class ResponsibilityItemV2 {
    ResponsibilityUid: string;
    AssetUid: string;
    Description: string;
    ResourceUid: string;
}