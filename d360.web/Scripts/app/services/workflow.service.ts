///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { WorkflowItem, WorkflowType, IWorkflowService, WorkflowTypeRelationEditorModel, Issue, SuggestedItem, CertifyItem } from '../models/workflow.model';
import { SelectItem, FormHelper } from '../models/form.model';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';

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
            .catch(this.handleError);
    }
    
}