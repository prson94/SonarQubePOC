
export interface IClaimsService {
    getClaims(objectID: number, objectType: string): Promise<ClaimItem[]>;
    getClaimsDisplayModel(objectID: number, objectType: string, responsibilityTypeID: number): Promise<ClaimsMatrixDisplayModel>;
    putClaims(objectID: number, objectType: string, responsibilityTypeID: number, claims: ClaimItem[]): Promise<any>;
}

export class ClaimItem {
    ResponsibilityTypeGroup: ResponsibilityTypeGroup;
    ResponsibilityTypeID: number;
    ObjectID: number;
    ObjectType: string;
    Name: string;
    Description: string;
}

export class ClaimsMatrixDisplayModel {
    ResponsibilityTypeID: number;
    Items: Array<ClaimsMatrixEditorItemModel>;
}

export class ClaimsMatrixEditorItemModel {
    Claim: Claim;
    ClaimObject: ClaimObject;
    ID: number;
}

export enum ResponsibilityTypeGroup {
    People = 1,
    Sourcing = 2,
}

export enum Claim {
        Read = 1,
        Create = 2,
        Update = 3,
        Delete = 4,
}

export enum ClaimObject {
        Root = 1,
        Attribute = 2,
        Governance = 3,
        Relationship = 4,
}