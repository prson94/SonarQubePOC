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

export enum WorkflowType {
    SuggestNewArtifact = 1,
    CertifyArtifact = 2,
    WorkIssue = 3,
    ChallengeArtifact = 4,
    SuggestNewArtifactMulti = 5,
}

export enum IssueType {
    Issue = 0,
    Challenge = 1
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