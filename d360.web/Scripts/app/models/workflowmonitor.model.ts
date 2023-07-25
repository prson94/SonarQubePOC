
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
	ObjectType: string;
	ObjectTypeID: number;
	Object: string;
	ObjectID: number;
	UID: string;
}

export class WorkflowMonitorItems {
    Items: WorkflowMonitorItem[];
    Total: number;
}