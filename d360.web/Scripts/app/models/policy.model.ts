import { NymType } from './object-detail.model';

export class PolicyType {
    Name: string;
    Description: string;   
    ID: number;
    PolicyTypeClass: string;
    PolicyTypeClassID: number;
    AllowAttributes: boolean;
    NymTypes: NymType[];
    MaximumDepth: number;
}

export enum PolicyStatus {
    Draft = 1,
    Active = 2,
    Inactive = 3
}

export class Policy {
    ID: number;
    ParentID: number;
    DisplayValue: string;
    Status: PolicyStatus;
    StatusName: string;
    Description: string;
    Level: number;
}