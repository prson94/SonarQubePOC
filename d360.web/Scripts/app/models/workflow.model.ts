import { SelectItem, FormMessage } from '../models/form.model';

export interface IWorkflowService {
    getWorkflow(id: number, workflowType: WorkflowType): Promise<WorkflowTypeRelationEditorModel>;
    getWorkflows(): Promise<WorkflowItem[]>;
    postWorkflow(workflow: WorkflowItem): Promise<any>;
    deleteWorkflow(id: number): Promise<any>;
    getResponsibilityTypeSelectList(id: number, type: string): Promise<SelectItem[]>;
    getParentTypeSelectList(id: number, type: string, workflowType: WorkflowType): Promise<SelectItem[]>;
}

export class WorkflowTypeRelationEditorModel {
    Enabled: boolean;
    ObjectTypes: SelectItem[];
    ParentTypes: SelectItem[];
    ResponsibilityTypes: SelectItem[];
    WorkflowType: WorkflowType;
    WorkflowTypeRelation: WorkflowItem;
}

export class WorkflowItem {
    ID: number;
    Object: string;
    ObjectID: number;
    Parent: string;
    ParentID: number;
    ParentName: string;
    WorkflowType: WorkflowType;
    Enabled: boolean;
    ResponsibilityTypeID: number;
    ResponsibilityType: string;
    WorkflowTypeName: string;
    WorkflowTypeDisplayName: string;
    Properties: string;
    Fields: any[];
}

export class Issue {
    Issue: string;
    IssueType: IssueType;
    IssueTypeName: string;
    ResourceName: string;
    ResourceID: number;
    ActivityName: string;
    DateStarted: string;
    WorkflowID: string;
    IssueID: number;
}

export class IssueDetail {
    ActivityName: string;
    AllowAction: boolean;
    DateCompleted: Date;
    DateStarted: Date;
    IsCompleted: boolean;
    Issue: string;
    Name: string;
    Notes: string;
    Object: string;
    ObjectID: number;
    RaisedBy: string;
    RaisedByResourceID: number;
    Url: string;
    WorkflowID: string;
    EllapsedDays: number;
}

export class CertifyItem {
    Name: string;
    ID: number;    
    Activity: number;
    ActivityName: string;
    ActivityDescription: string;
    StartDate: string;
    DueDate: string;
    WorkflowID: string;
    TypeName: string;
}

export class SuggestedItem {
    Name: string;
    ID: number;
    ProposedName: string;
    ProposedDescription: string;
    TaxonomyTypeName: string;
    RequestingResourceName: string;
    RequestingResourceID: number;
    ActivityName: string;
    StartDate: string;
    WorkflowID: string;
    Activity: number;
}

export class WorkflowStepStatistic {
    Count: number;
    ID: number;
    Name: string;
    WorkflowType: number;
}

export class ArtifactTypeWorkflowBreakdown {
    Description: string;
    Name: string;
    ID: WorkflowType;
    Steps: WorkflowStepStatistic[];    
}

export class WorkflowStep {
    Date: Date;
    ID: number;
    Name: string;
    TraceLevel: string;
}

export class WorkflowStatusDetailField {
    Name: string;
    Value: string;
}

export class WorkflowAssignment {
    ActivityType: number;
    ActivityTypeDescription: string;
    ActivityTypeName: string;
    IsComplete: boolean;
    ResourceID: number;
    ResourceName: string;
}

export class WorkflowStatusDetails {
    Assignments: WorkflowAssignment[];
    DateCompleted: Date;
    DateStarted: Date;
    Fields: WorkflowStatusDetailField[];
    ID: string;
    Steps: WorkflowStep[];
    WorkflowType: WorkflowType;
    WorkflowTypeDescription: string;
    WorkflowTypeName: string;
}

export class WorkflowIssueType {
    ID: number;
    Name: string;
    Description: string;
    IsSystem: boolean;
}

export class IssueInfo {
    Fields: any[];
    Issue: any;
}


export class WorkflowTypeNew
{
    ID: number;
    Name: string;
    CreatedBy: number;
    CreatedOn: string;
    UpdatedBy: number;
    UpdatedOn: string;
    PublishedVersionID: number;
    Deleted: boolean = false;
}


//#region diagram

export class WorkflowDiagramModel {
    Type: WorkflowTypeNew = new WorkflowTypeNew();
    Event: WorkflowEventRegistration = new WorkflowEventRegistration();
    Nodes: WorkflowDiagramNode[] = [];
    Links: WorkflowDiagramLink[] = [];

}

export class WorkflowDiagramNode {
    Key: string;
    XPosition: string;
    YPosition: string;
    StepType: StepType;
    ActivityType: number;
    ActivityTypeInfo: ActivityTypeInfo;
    Settings: string;
    SettingsObject: any;
    Name: string;
}

export class WorkflowDiagramLink {
    Key: string;
    FromKey: string;
    ToKey: string;
    TransitionType: TransitionType;
    Condition: string;
    ConditionObject: any;
    Name: string;
}

export class LinkModel {
    key: string;
    from: string;
    to: string;
    name: string;
    category: string = '';
    //template: string = '';
    diagramObjectType: DiagramObjectType = DiagramObjectType.Link;
    fromportid: string;
    toportid: string;

    transitionType: TransitionType;
    condition: any;

}

export class NodeModel {
    key: string;
    name: string;
    pos: string;
    //template: string = 'task';
    category: string = 'task';

    diagramObjectType: DiagramObjectType = DiagramObjectType.Node;

    x: string;
    y: string;
    stepType: StepType;
    activityType: number;
    settings: any;
    fore: string;
    back: string;
    icon: string;
    activityDescription: string;
    activityName: string;
}

export class ActivityTypeInfo {
    ID: number;
    Name: string;
    Description: string;
    BackColor: string;
    ForeColor: string;
    Icon: string;

}

//#endregion

export class WorkflowFormField {
    Label: string;
    FieldType: WorkflowFormFieldType;   
    Value: any;
    ID: string;
}

export class WorkflowForm {
    Fields: WorkflowFormField[]=[];
    Title: string;
    Description: string;
}

export class WorkflowListItem {
    ID: number;
    CreatedOn: string;
    CreatedBy: string;
    UpdatedOn: string;
    UpdatedBy: string;
    Name: string;
    TypeName: string;
    ChangeType: WorkflowChangeType;
    Published: string;
}

export class WorkflowEventRegistration {
    ID: number = 0;
    TypeID: number;
    Object: string;
    ObjectID: number;
    ChangeType: WorkflowChangeType;
    Condition: string;
    ConditionObject: any;

    conditions: EventCondition[] = [];
}

export class WorkflowObjectType {
    value: string;
    id: number;
    type: string;
    name: string;
}

export class ChangeTypeInfo {
    ID: number;
    Name: string;
    Description: string;
}

export class EventCondition {
    FieldTypeID: number = 0;
    Value: any;
    ValueType: string;
    Operator: string;

    fieldName: string;
}

//#region enums

export enum WorkflowChangeType {
    Add = 1,
    Update = 2,
    Delete = 3,
    Schedule = 4
}

export enum WorkflowFormFieldType {
    Text = 0,
    Boolean = 1,
    Integer = 2,
    Date
}

export enum WorkflowActivityType {
    None = 1,
    EmailNotification = 1,
    StatusChange = 2,
    Form = 3
}

export enum DiagramObjectType {
    Link,
    Node
}

export enum StepType {
    Start = 1,
    Task = 2,
    Terminate = 3,
    Finish = 4
}

export enum TransitionType {
    Always = 1,
    Condition = 2,
    Link = 3
}

export enum IssueType {
    Issue = 0,
    Challenge = 1
}

export enum WorkflowType {
    SuggestNewArtifact = 1,
    CertifyArtifact = 2,
    WorkIssue = 3,
    ChallengeArtifact = 4,
    SuggestNewArtifactMulti = 5,
}

//#endregion