
export class WorkflowMonitorItem {
    ID: number;
    WorkflowName: string;
    Name: string;
    TypeName: number;
    Type: string;
    StartedOn: Date;
    CompletedOn: Date;
   
}

export class WorkflowMonitorItems {
    Items: WorkflowMonitorItem[];
    Total: number;
}