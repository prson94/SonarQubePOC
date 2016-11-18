import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { WorkflowStatusDetails, WorkflowItem, WorkflowType, IWorkflowService, WorkflowTypeRelationEditorModel, Issue, IssueDetail, SuggestedItem, CertifyItem, ArtifactTypeWorkflowBreakdown } from '../models/workflow.model';
import { SelectItem, FormHelper } from '../models/form.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';
import { DynamicGridResultsInData } from '../models/grid-definition.model';

@Injectable()
export class WorkflowService extends BaseService implements IWorkflowService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService);}

    getWorkflows(): Promise<WorkflowItem[]> {
        return this.http.get('/api/workflows/relations')
            .toPromise()
            .then(response => <WorkflowItem[]>response.json())
            .catch(err=>this.handleError(err));
    }

    getWorkflow(id: number, workflowType: WorkflowType): Promise<WorkflowTypeRelationEditorModel> {
        return this.http.get(`form/WorkflowAllocation?id=${id}&workflowType=${workflowType}`)
            .toPromise()
            .then(response => <WorkflowTypeRelationEditorModel>response.json())
            .catch(err =>this.handleError(err));
    }

    postWorkflow(workflow: WorkflowItem): Promise<any>  {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        return this.http.post('form/WorkflowAllocation', JSON.stringify(workflow), { headers: headers })
            .toPromise()
            .catch(err =>this.handleError(err));
    }

    deleteWorkflow(id: number): Promise<any> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        return this.http.delete(`/form/DeleteWorkflowAllocationByID?id=${id}`, headers)
            .toPromise()
            .catch(err =>this.handleError(err));
    }

    getResponsibilityTypeSelectList(id: number, type: string): Promise<SelectItem[]> {
        return this.http.get(`/workflow/WorkflowResponsibilityTypeOptions?type=${type}&id=${id}`)
            .toPromise()
            .then(response => <SelectItem[]>response.json())
            .then(r => {
                FormHelper.mapSelectItems(r);
                return r;
            })
            .catch(err =>this.handleError(err));
    }

    getParentTypeSelectList(id: number, type: string, workflowType: WorkflowType): Promise<SelectItem[]> {
        return this.http.get(`/workflow/WorkflowParentTypeOptions?workflowType=${workflowType}&type=${type}&id=${id}`)
            .toPromise()
            .then(response => <SelectItem[]>response.json())
            .then(r => {
                FormHelper.mapSelectItems(r);
                return r;
            })
            .catch(err =>this.handleError(err));
    }

    getMyCounts(daysToLookBack: number, resourceId?: number) : Promise<Count[]> {
        return this.http.get(`api/count/assignments/${daysToLookBack}` + (resourceId ? `?id=${resourceId}` : ''))
            .toPromise()
            .then(response => <Count[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSuggestedItems(objectID: number, objectType: string): Promise<SuggestedItem[]> {
        let url = 'services/workflow/tasks/types/1/';

        if (objectID > 0 && objectType != undefined) {
            url += `${objectID}/${objectType}`;
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <SuggestedItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    getCertifyItems(objectID: number, objectType: string): Promise<CertifyItem[]> {
        let url = 'services/workflow/tasks/types/2/';

        if (objectID > 0 && objectType != undefined) {
            url += `${objectID}/${objectType}`;
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <CertifyItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    exportAllIssueDetails() {
        window.location.assign('services/workflow/all/issues/excel/excel.xls');        
    }

    getAllIssueDetails(): Promise<IssueDetail[]> {
        let url = 'services/workflow/all/issues?$orderby=DateStarted%20desc,Issue';
        
        return this.http.get(url)
            .toPromise()
            .then(response => <IssueDetail[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowDetails(workflowId: string): Promise<any> {
        let url = `services/workflow/tasks/${workflowId}`;

        return this.http.get(url)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    getIssues(objectID: number, objectType: string): Promise<Issue[]> {
        let url = 'services/workflow/tasks/types/3/';

        if (objectID > 0 && objectType != undefined) {
            url += `${objectID}/${objectType}`;
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <Issue[]>response.json())
            .catch(err => this.handleError(err));
    }

    updateIssue(issue: Issue, action: string, comment: string, assignTo?: string): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        return this.http
            .post(`/services/workflow/tasks/${issue.WorkflowID}`, JSON.stringify({ WorkflowAction: action, AssignTo: assignTo, Comment: comment }), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err=>this.handleError(err));
    }

    updateSuggestion(suggestion: SuggestedItem, approve: boolean, comments: string): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        return this.http
            .post(`/services/workflow/tasks/${suggestion.WorkflowID}`, JSON.stringify({
                WorkflowAction: 'ApprovalFromOwner', Approved: approve, Notes: comments }), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    certifyArtifact(certify: CertifyItem): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        return this.http
            .post(`/services/workflow/tasks/${certify.WorkflowID}`, JSON.stringify({ WorkflowAction: 'CertificationFromOwner' }), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    raiseIssue(objectId: number, objectType: string, issue: string, type: string): Promise<any> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        return this.http
            .post(`/api/issue/raise/${objectType}/${objectId}/${type}`, JSON.stringify(issue), { headers: headers })
            .toPromise()
            .then(res => <any>res)
            .catch(err => this.handleError(err));
    }

    getWorkflowStepBreakdownByArtifactType(artifactTypeId: number): Promise<ArtifactTypeWorkflowBreakdown[]> {
        return this.http.get(`workflow/WorkflowStepBreakdownByArtifactType?id=${artifactTypeId}`)
            .toPromise()
            .then(response => <ArtifactTypeWorkflowBreakdown[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowsByArtifactTypeAndStep(artifactTypeId: number, workflowTypeId: number, stepId: number): Promise<DynamicGridResultsInData>{
        return this.http.get(`workflow/WorkflowsByArtifactTypeAndWorkflowTypeAndStep?id=${artifactTypeId}&type=${workflowTypeId}&step=${stepId}&isNg=true`)
            .toPromise()
            .then(response => <DynamicGridResultsInData>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowStatus(workflowId: string): Promise<WorkflowStatusDetails> {
        return this.http.get(`services/workflow/${workflowId}/status`)
            .toPromise()
            .then(response => <WorkflowStatusDetails>response.json())
            .catch(err => this.handleError(err));
    }
}