import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import {
    IssueInfo,
    WorkflowStatusDetails,
    WorkflowItem,
    WorkflowType,
    IWorkflowService,
    WorkflowTypeRelationEditorModel,
    Issue,
    IssueDetail,
    SuggestedItem,
    CertifyItem,
    ArtifactTypeWorkflowBreakdown,
    WorkflowIssueType,
    WorkflowDiagramModel,
    ActivityTypeInfo,     
    WorkflowForm,
    WorkflowTypeItem,
    WorkflowObjectType,
    ChangeTypeInfo,
    WorkflowEventRegistration,
} from '../models/workflow.model';
import { FieldType } from '../models/fields.model';
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

    getSuggestedItemsForUser(resourceID: number): Promise<SuggestedItem[]> {
        return this.http.get(`services/workflow/tasks/types/1?resourceID=${resourceID}`)
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

    getCertifyItemsForUser(resourceID: number): Promise<CertifyItem[]> {
        return this.http.get(`services/workflow/tasks/types/2?resourceID=${resourceID}`)
            .toPromise()
            .then(response => <CertifyItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    exportAllIssueDetails(all?: boolean) {
        window.location.assign(`services/workflow/all/issues/excel/excel.xls?all=${(all === undefined || all) ? 'true':'false'}`);        
    }

    getAllIssueDetails(all?: boolean): Promise<IssueDetail[]> {
        let url = `services/workflow/${(all === undefined || all) ? 'all':'my'}/issues?$orderby=DateStarted%20desc,Issue`;
                
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

    getIssuesForUser(resourceID: number): Promise<Issue[]> {
        return this.http.get(`services/workflow/tasks/types/3?resourceID=${resourceID}`)
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

    raiseIssue(issue: any): Promise<JsonResult> {
        return this.postDynamic(this.http, 'issue', issue);
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

    getWorkflowIssueTypes(): Promise<WorkflowIssueType[]> {
        return this.http.get('api/issuetypes')
            .toPromise()
            .then(response => <WorkflowIssueType[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteWorkflowIssueType(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'ISSUETYPE', id);
    }

    saveIssueType(issueType: WorkflowIssueType): Promise<JsonResult> {
        if (issueType.ID == undefined || !issueType.ID) {
            return this.postDynamic(this.http, 'issuetype', issueType);
        }
        return this.putDynamic(this.http, 'issuetype', issueType);
    }

    getIssueDetails(issueId: number): Promise<IssueInfo> {
        return this.http.get(`api/issue/${issueId}`)
            .toPromise()
            .then(response => <IssueInfo>response.json())
            .catch(err => this.handleError(err));
    }

    //#region diagram

    public getWorkflowDiagram(id: number): Promise<WorkflowDiagramModel> {
        return this.http.get(`services/workflow/diagram/${id}`)
            .toPromise()
            .then(response => <WorkflowDiagramModel>response.json())
            .catch(err => this.handleError(err));
    }

    //#endregion

    getWorkflowForm(id: number, itemStepId: number): Promise<WorkflowForm> {
        return this.http.get(`/services/workflow/form/${id}/${itemStepId}`)
            .toPromise()
            .then(response => <WorkflowForm>response.json())
            .catch(err => this.handleError(err));
    }

    submitWorkflowForm(itemId: number, stepId: number, fields: any[]): Promise<any> {
        return this.http
            .post(`services/workflow/SubmitWorkflowForm/${itemId}/${stepId}`, fields)
            .toPromise()
            .then(res => <any>res.json())
            .catch(err => this.handleError(err));
    }

    getActivityTypes(): Promise<ActivityTypeInfo[]> {
        return this.http.get('services/workflow/activitytypes')
            .toPromise()
            .then(response => <ActivityTypeInfo[]>response.json())
            .catch(err => this.handleError(err));
    }

    getChangeTypes(): Promise<ChangeTypeInfo[]> {
        return this.http.get('services/workflow/changetypes')
            .toPromise()
            .then(response => <ChangeTypeInfo[]>response.json())
            .catch(err => this.handleError(err));
    }
    
    getTypes(): Promise<any> {
        return this.http.get('services/workflow/types')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getObjectTypes(objectID: number, objectType: string): Promise<WorkflowTypeItem[]> {
        return this.http.get(`services/workflow/types/${objectID}/${objectType}`)
            .toPromise()
            .then(response => <WorkflowTypeItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowItems(typeId: number) : Promise<any[]> {
        return this.http.get(`services/workflow/items/${typeId}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowObjectTypes(): Promise<WorkflowObjectType[]> {
        return this.http.get('services/workflow/objecttypes')
            .toPromise()
            .then(response => <WorkflowObjectType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowFieldTypes(id: number, type: string): Promise<FieldType[]> {
        return this.http.get(`services/workflow/fieldtypes/${type}/${id}`)
            .toPromise()
            .then(response => <FieldType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowItemDetails(typeId: number, object: string, objectId: number): Promise<any[]> {
        return this.http.get(`services/workflow/item/details/${typeId}/${object}/${objectId}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    
    saveWorkflowDiagramModel(model: WorkflowDiagramModel): Promise<number> {
        //returns workflowtype id
        return this.http.post('services/workflow/diagram/save', model)
            .toPromise()
            .then(response => <number>response.json())
            .catch(err => this.handleError(err));
    }

    getLookupList(id: number): Promise<any[]> {
        return this.http.get(`api/lookup/list/${id}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionLookupList(id: number): Promise<any[]> {
        return this.http.get(`api/fusionlookup/list/${id}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteWorkflowType(id: number) {
        return this.http.delete(`services/workflow/type/${id}/delete`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

}