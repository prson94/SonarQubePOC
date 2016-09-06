///<reference path="../es6-shim.d.ts"/>
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
}

export class Issue {
    Issue: string;
    ResourceName: string;
    ResourceID: number;
    ActivityName: string;
    DateStarted: string;
    WorkflowID: string;
}

export class SuggestedItem {
    Name: string;
    ID: number;
    ProposedName: string;
    TaxonomyTypeName: string;
    RequestingResourceName: string;
    RequestingResourceID: number;
    ActivityName: string;
    StartDate: string;
    WorkflowID: string;
}