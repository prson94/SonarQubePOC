///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { WorkflowItem, WorkflowType, IWorkflowService, WorkflowTypeRelationEditorModel } from '../models/workflow.model';
import { SelectItem, FormHelper } from '../models/form.model';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Count } from '../models/counts.model';

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

    getMyCounts(daysToLookBack: number) : Promise<Count[]> {
        return this.http.get(`api/count/assignments/${daysToLookBack}`)
            .toPromise()
            .then(response => <Count[]>response.json())
            .catch(err => this.handleError(err));
    }
    
}