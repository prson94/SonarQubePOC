import { SelectItem, FormMessage } from '../models/form.model';
import { State } from './asset.model';


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
    WorkflowItemID: number;
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
    Description: string;
    CreatedBy: number;
    CreatedOn: string;
    UpdatedBy: number;
    UpdatedOn: string;
    PublishedVersionID: number;
    Deleted: boolean = false; 
    State: State = State.Active;
}


//#region diagram

export class WorkflowDiagramModel {
    Type: WorkflowTypeNew = new WorkflowTypeNew();
    Event: WorkflowEventRegistration = new WorkflowEventRegistration();
    Nodes: WorkflowDiagramNode[] = [];
    Links: WorkflowDiagramLink[] = [];

    CurrentVersion: number;
    PublishedVersion: number;

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
    RunCount: number;
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

    valid: boolean = true;
    errors: string[] = [];
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
    runCount: number;

    settings: any = {};
    fields: any = {};

    hasMultipleInputs: boolean = false;
    valid: boolean = false;
    errors: string[] = [];
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
    AllowMultipleValues: boolean;

}

export class WorkflowForm {
    Fields: WorkflowFormField[]=[];
    Title: string;
    Description: string;
    IsCompleted: boolean;
    IsItemDeleted: boolean;
    IsUserAllowedToComplete: boolean;
    ObjectName: string;
    ObjectType: string;
    ObjectTypeID: number;
    ObjectID: number;
    IssueObject: string;
    IssueObjectID: number;
    IssueObjectName: string;
    IssueTypeName: string;
    TypeName: string;
    AllowReassignObject: boolean;
    AllowReassignResource: boolean;
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
    VersionID: number;
    ItemID: number;
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
    LastExecuted: any;
    conditions: EventCondition[] = [];
}

export class WorkflowObjectType {
    value: string;
    id: number;
    type: string;
    label: string;
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
    Loaded = 7,
    RequestCertification = 8,
}

export enum WorkflowFormFieldType {
    Text = 0,
    Boolean = 1,
    Integer = 2,
    Date = 3,
    TextArea = 4,
    List = 5,
    RelationshipType = 6,
}

export enum WorkflowActivityType {
    None = 0,
    EmailNotification = 1,
    StatusChange = 2,
    Form = 3,
    Procedure = 4,
    FieldChange = 5,
    RelationshipUpdate = 6,
    StateChange = 7,
    Delete = 8
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

export enum EmailTaskRecipientType {
    None = 0,
    Initiator,
    Responsibility,
    SpecificUser
}

//#endregion

export class WorkflowAssignmentSummary {

    Version: number;
    StepName: string;
    ObjectName: string;
    TypeName: string;
}

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

export class WorkflowItemStep {
    ID: number;
    ItemID: number;
    StepID: number;
    Name: string;
    StepType: StepType;
    ActivityType: WorkflowActivityType;
    Assignee: string;
    Complete: boolean;
    StartedOn: string;
    StartedBy: string;
    CompletedOn: string;
    CompletedBy: string;
    IsIssueType: boolean;
    Object: string;
    ObjectID: number;
    TypeID: number;
}

export class BulkWorkflowFormModel {
    ItemStepIDs: number[] = [];
    AsigneeResourceID: number = 0;
    Fields: WorkflowFormField[] = [];
}

export class EmailTaskRecipientTypeInfo {
    ID: number;
    Name: string;
}

export class WorkflowStepDetail {
    ID: number;
    StepType: StepType;
    ActivityType: WorkflowActivityType;
    SettingsXml: string;
    FieldsXml: string;
    Settings: any;
    Fields: any;
    ItemSettingsXml: string;
    ItemFieldsXml: string;
    ItemSettings: any;
    ItemFields: any;
    Name: string;
    ObjectType: string;
    ObjectTypeID: number;
    ObjectTypeName: string;
    Object: string;
    ObjectID: number;
    ObjectName: string;
    ChangeType: WorkflowChangeType;
    ConditionXml: string;
    Condition: any;
    EventSettingsXml: string;
    EventSettings: any;
    IsIssueType: boolean;
    Version: number;
    IsPublishedVersion: boolean;
    IssueDetails: WorkflowStepIssueDetail;
    AssignedUsers: any[] = [];
    StepID: number;
    TypeID: number;
    IsAssignedLoginUser: boolean;
    ItemID: number;
    ItemStepID: number;
    FieldChanges: WorkflowStepFieldChangeDetail[];
    RelationshipChange: WorkflowStepRelationshipChangeDetail;
    StateChange: State;
}


export class WorkflowStepIssueDetail {
    ID: number;
    IssueID: number;
    IssueTypeID: number;
    IssueName: string;
    ObjectName: string;
    ObjectTypeName: string;
    Object: string;
    ObjectID: number;
    ObjectType: string;
    ObjectTypeID: number;
}

export class WorkflowStepFieldChangeDetail {
    FieldValue: string;
    FieldName: string;
    Asset: string;
    Type: string;
    Value: string;
    UseCurrentDate: boolean;
    FormValue: string;
    AppendValue: string;
    ClearValue: string;
}


export class WorkflowStepRelationshipChangeDetail {
    TypeName: string;
    Relationship: string;
    AppendValue: boolean;
    ClearValue: boolean;
}