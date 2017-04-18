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
    Fields: string;
    FieldsObject: any;
    Name: string;
}

export class WorkflowDiagramLink {
    Key: string;
    FromKey: string;
    ToKey: string;
    FromPortID: string;
    ToPortID: string;
    TransitionType: TransitionType;
    Condition: string;
    ConditionObject: any;
    Settings: string;
    SettingsObject: any;
    Name: string;
}

export class LinkModel {
    key: string;
    from: string;
    to: string;
    name: string;
    category: string = '';
    diagramObjectType: DiagramObjectType = DiagramObjectType.Link;
    frompid: string;
    topid: string;
    icon: string;

    transitionType: TransitionType = TransitionType.Always;
    condition: any = [];
    settings: any = {};
    formInputs: any = [];
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
    fore: string;
    back: string;
    icon: string;
    activityDescription: string;
    activityName: string;


    settings: any = {};
    fields: any = {};

    hasMultipleInputs: boolean = false;
}


export class ActivityTypeInfo {
    ID: number;
    Name: string;
    Description: string;
    BackColor: string;
    ForeColor: string;
    Icon: string;

}

export class TransitionTypeInfo {
    ID: Number;
    Name: string;
    Description: string;
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
    IsCompleted: boolean;
    ObjectName: string;
    ObjectType: string;
    ObjectID: number;
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
    NumberOfEvents: number;

    ChangeTypeName: string;
}

export class WorkflowEventRegistration {
    ID: number = 0;
    TypeID: number;
    Object: string;
    ObjectID: number;
    ChangeType: WorkflowChangeType;
    Condition: string;
    ConditionObject: any = {};
    Settings: string;
    SettingsObject: any = {};

    conditions: EventCondition[] = [];
}

export class WorkflowObjectType {
    value: string;
    id: number;
    type: string;
    name: string;
    count: number;
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

    //TODO: explore as alternative to mapping manually
    //get FieldTypeID(): number {
    //    return +this['@FieldTypeID'];
    //}

    //set FieldTypeID(val: number) {
    //    this['@FieldTypeID'] = val;
    //}
}


export class WorkflowTaskProcedure {
    ID: number;
    Name: string;
    Procedure: string;
    PassObjectInfo: boolean;
    UpdatedBy: number;
    UpdatedOn: string;
}

//#region enums

export enum WorkflowChangeType {
    Add = 1,
    Update = 2,
    Delete = 3,
    Schedule = 4,
    ScoreUpdate = 5,
    RuleResult = 6,
    Loaded = 7
}

export enum WorkflowFormFieldType {
    Text = 0,
    Boolean = 1,
    Integer = 2,
    Date = 3,
    TextArea = 4
}

export enum WorkflowActivityType {
    None = 1,
    EmailNotification = 1,
    StatusChange = 2,
    Form = 3,
    Procedure = 4,
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
    Timer = 3
}

export enum IssueType {
    Issue = 0,
    Challenge = 1
}

export enum WorkflowType {
    None = 0,
    SuggestNewArtifact = 1,
    CertifyArtifact = 2,
    WorkIssue = 3,
    ChallengeArtifact = 4,
    SuggestNewArtifactMulti = 5,
}

export enum FormResponseType {
    FirstResponse = 0,
    All = 1,
    Majority = 2
}

//#endregion


export class WorkflowAssignmentDetail {
    ItemID: number;
    ItemStepID: number;
    Object: string;
    ObjectID: number;
    ObjectName: string;
    ObjectType: string;
    ObjectTypeID: number;
    StartedBy: string;
    StartedByResourceID: number;
    StartedOn: Date;
    TypeName: string;
    WorkflowName: string;
    StepName: string;
    StepType: StepType;    
    ActivityType: WorkflowActivityType;
}