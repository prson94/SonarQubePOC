export class LoadDetail {
    ID: number;
    Object: string;
    ObjectID: number;
    ObjectName: string;
    Notes: string;
    FilePath: string;
    DateStarted: string;
    DateCompleted: string;
    Action: string;
    Success: number;
    Error: number;
    Incomplete: number;
    Total: number;
    Requestor: string;
}

export class LoadFilePostModel {
    LoadAction: string;
    Type: string;
    Notes: string;
    File: string;
}

export class LoadColumn {
    Name: string;
    Required: boolean;
    PartOfKey: boolean;
    IsLookup: boolean;
    Lookups: LoadColumnValue[];
}

export class LoadColumnValue {
    Value: string;
}

export class LoadItemsModel {
    pageNum: number;
    pageSize: number;
    total: number;
    items: any[];
}
