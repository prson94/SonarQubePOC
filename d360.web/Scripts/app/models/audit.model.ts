export class Audit {
    Action: string;
    ActionDescription: string;
    ActionObject: string;
    ActionObjectID: number;
    ActionObjectName: string;
    ActionObjectTypeName: string;
    Date: Date;
    ID: number;
    Object: string;
    ObjectID: number;
    ObjectName: string;
    ResourceID: number;
    ResourceName: string;
    Field: string;
    NewValue: string;
    Version: string;
}

export class AuditResults {
    results: Audit[];
    total: number;
}