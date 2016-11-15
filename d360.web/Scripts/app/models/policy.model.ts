export class PolicyType {
    Name: string;
    Description: string;   
    ID: number;
    PolicyTypeClass: string;
    PolicyTypeClassID: number;
    AllowAttributes: boolean;
}

export enum PolicyStatus {
    Draft = 1,
    Active = 2,
    Inactive = 3
}

export class Policy {
    ID: number;
    ParentID: number;
    Name: string;
    Status: PolicyStatus;
    StatusName: string;
    Description: string;
}