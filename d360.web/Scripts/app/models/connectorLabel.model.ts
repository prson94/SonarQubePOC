export class ConnectorLabel {
    uid: string;
    Value: string;
    UseCount: number;
    State: ConnectorLabelState;
    CreatedOn: Date;
    CreatedBy: string;
    UpdatedOn: Date;
    UpdatedBy: string;
    TooltipID: number;
}

export enum ConnectorLabelState {
    Unknown = -1,
    PendingAdd = 0,
    Active = 1,
    PendingDelete = 2,
    Deleted = 3,
    InActive = 4
} 