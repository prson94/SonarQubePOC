import { NymType } from './object-detail.model';

export class PolicyType {
    Name: string;
    Description: string;   
    ID: number;
    AssetTypeID: number;
    AllowAttributes: boolean;
    NymTypes: NymType[];
    MaximumDepth: number;
    AssetTypeUID: string;
}

export class Policy {
    ID: number;
    Uid: string;
    AssetID: number;
    ParentID: number;
    DisplayValue: string;    
    Description: string;
    Level: number;
    HasWorkflow: boolean;
}