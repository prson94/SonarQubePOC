export class Tag {
    IconBackColor: string;
    IconForeColor: string;
    Object: string;
    ObjectID: number;
    ObjectTypeName: string;
    TextPath: string;
    Url: string;
}

export class TagType {
    uid: string;
    Value: string;
    UseCount: number;
    State: TagTypeState;
    CreatedOn: Date;
    CreatedBy: string;
    UpdatedOn: Date;
    UpdatedBy: string;
}

export enum TagTypeState {
    Unknown = -1,
    PendingAdd = 0,
    Active = 1,
    PendingDelete = 2,
    Deleted = 3,
    InActive = 4
} 

export class TagDetail {
    DisplayValue: string;
    AssetId: number;
    AssetType: string;
    Object: string;
    ObjectID: number;
    TagsAsString: string;
    Tags: TagItem[];
}
export class TagItem {
    Uid: number;
    Value: string;
}