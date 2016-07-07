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
