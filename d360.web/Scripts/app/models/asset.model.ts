

export enum State {
    Unknown = -1,
    PendingAdd = 0,
    Active = 1,
    PendingDelete = 2,
    Deleted = 3,
    InActive = 4
}

export class AssetEditorModel {
    Uid: string;
    ParentUid: string;
    Fields: any;
}

export class AssetDetail {
    AssetTypeID: number;
    AssetTypeName: string;
    CreatedOn: Date;
    DisplayValue: string;
    ID: number;
    Object: string;
    ObjectID: number;
    State: number;
    Type: string;
    TypeID: number;
    UpdatedOn: Date;
}