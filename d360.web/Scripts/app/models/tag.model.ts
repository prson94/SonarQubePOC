export class Tag {
    IconBackColor: string;
    IconForeColor: string;
    Object: string;
    ObjectID: number;
    ObjectTypeName: string;
    TextPath: string;
    Url: string
    Displayobject: string;
    AssetUid: string;
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
    TooltipID: number;
    CreatedByFirstName?: string;
    CreatedByLastName?: string;
}

export class TagApiModel {
    TagUID: string;
    AssetUID: string;
    TagName: string;
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
    AddedBy?: string;
    AddedByUid?: string;
    DisplayValue: string;
    AssetId: number;
    AssetUid: string;
    AssetType: string;
    Object: string;
    ObjectID: number;
    TagsAsString: string;
    Tags: TagItem[];
    HasProfiling?: any;
}

export interface TagDetailResponse {
    items: TagDetail[];
    pageNum: number;
    pageSize: number;
}

export class TagItem {
    Uid: number;
    CreatedByFirstName: string;
    CreatedByLastName: string;
    CreatedByUid: string;
    CreatedOn: string;
    Value: string;
    uid: string;
}

export class TagPermissionItem {
    Uid: number;
    Value: string;
    CanDelete: boolean;
}