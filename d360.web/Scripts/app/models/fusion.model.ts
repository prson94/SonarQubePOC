export class FusionType {
    AssetTypeID: number;
    ID: number;
    Name: string;
    Description: string;
    UpdatedOn: string;
    UpdatedBy: number;
    uid: string;
}

export class FusionAttributeType {
    AssetTypeID: number;
    ID: number;
    ParentID: number;
    FusionTypeID: number;
    Assignable: boolean;
    ScanEnabled: boolean;
    Name: string;
    Path: string;
    TextPath: string;
    UpdatedOn: string;
    UpdatedBy: number;
}
