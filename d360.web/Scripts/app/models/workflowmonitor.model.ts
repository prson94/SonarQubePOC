
export class WorkflowMonitorItem {
    Id: number;
    WorkflowName: string;
    Type: string;
    TypeName: number;
    Asset: string;
    Initiator:string;
    StartedOn: Date;
    CompletedOn: Date;
    Status: string;
   
}

export class WorkflowMonitorItems {
    Items: WorkflowMonitorItem[];
    Total: number;
}