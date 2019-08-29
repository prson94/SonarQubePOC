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

export enum PolicyStatus {
    Draft = 1,
    Active = 2,
    Inactive = 3
}

export class Policy {
    ID: number;
    Uid: string;
    AssetID: number;
    ParentID: number;
    DisplayValue: string;
    Status: PolicyStatus;
    StatusName: string;
    Description: string;
    Level: number;
}