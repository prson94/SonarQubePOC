import { Injectable } from '@angular/core';
import { Headers, Http, Response, ResponseContentType } from '@angular/http';
import {
    IssueInfo,
    WorkflowStatusDetails,
    WorkflowItem,
    WorkflowType,    
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
    WorkflowListItem,
    WorkflowObjectType,
    ChangeTypeInfo,
    WorkflowEventRegistration,
    TransitionTypeInfo,
    WorkflowTaskProcedure,   
    EmailTaskRecipientTypeInfo,
    WorkflowChangeType,
    BulkWorkflowFormModel,
    WorkflowItemStep,
    BulkWorkflowReassignModel,
} from '../models/workflow.model';
import { FieldType } from '../models/fields.model';
import { SelectItem, FormHelper } from '../models/form.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';
import { DynamicGridResultsInData } from '../models/grid-definition.model';

@Injectable()
export class WorkflowService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService);}
    
    getMyCounts(daysToLookBack: number, resourceId?: number) : Promise<Count[]> {
        return this.http.get(`api/count/assignments/${daysToLookBack}` + (resourceId ? `?id=${resourceId}` : ''))
            .toPromise()
            .then(response => <Count[]>response.json())
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

    getIssues(objectID: number, objectType: string): Promise<Issue[]> {
        let url = 'services/workflow/issue/type/';

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
  
    raiseIssue(issue: any): Promise<JsonResult> {
        return this.postDynamic(this.http, 'issue', issue);
    }
   
    getWorkflowIssueTypes(object: string = null, objectId: number = null): Promise<WorkflowIssueType[]> {
        let url = 'api/issuetypes';
        if (object != null && objectId != null)
            url += `?object=${object}&objectID=${objectId}`
        return this.http.get(url)
            .toPromise()
            .then(response => <WorkflowIssueType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAdminWorkflowIssueTypes(): Promise<WorkflowIssueType[]> {
        return this.http.get('api/adminissuetypes')
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

    public getWorkflowDiagram(id: number, version?: number, filteredObject?: string, filteredObjectId?: number): Promise<WorkflowDiagramModel> {
        let uri = `services/workflow/diagram/${id}${version != null ? '?version=' + version : ''}`

        if (filteredObject != null && filteredObjectId != null)
            uri += `${version == null ? '?' : '&'}filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`

        return this.http.get(uri)
            .toPromise()
            .then(response => <WorkflowDiagramModel>response.json())
            .catch(err => this.handleError(err));
    }

    //#endregion


    //#region bulk

    getWorkflowBulkForm(model: BulkWorkflowFormModel) {
        return this.http.post('/services/workflow/form/bulk', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    submitBulkWorkflowForm(model: BulkWorkflowFormModel) {
        return this.http.post('/services/workflow/SubmitWorkflowForm/bulk', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    //#endregion

    getWorkflowForm(id: number, itemStepId: number): Promise<WorkflowForm> {
        return this.http.get(`/services/workflow/form/${id}/${itemStepId}`)
            .toPromise()
            .then(response => <WorkflowForm>response.json())
            .catch(err => this.handleError(err));
    }
 
    reassignUser(itemStepId: number, resourceId: number): Promise<JsonResult> {
        return this.http
            .post(`services/workflow/ReassignWorkflowResource/${itemStepId}/${resourceId}`, null)
            .toPromise()
            .then(res => <any>res.json())
            .catch(err => this.handleError(err));
    }

    reassignObject(itemId: number, workflowId: number, objectId: number, objectType: string, stepId:number): Promise<JsonResult> {
        return this.http
            .post(`services/workflow/ReassignWorkflowObject/${itemId}/${workflowId}/${objectId}/${objectType}/${stepId}`,null)
            .toPromise()
            .then(res => <any>res.json())
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

    getTransitionTypes(): Promise<TransitionTypeInfo[]> {
        return this.http.get('services/workflow/transitiontypes')
            .toPromise()
            .then(response => <TransitionTypeInfo[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAdminTypes(): Promise<any> {
        return this.http.get('services/workflow/admintypes')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getTypes(): Promise<any> {
        return this.http.get('services/workflow/types')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getObjectTypes(objectID: number, objectType: string): Promise<WorkflowListItem[]> {
        return this.http.get(`services/workflow/types/${objectID}/${objectType}`)
            .toPromise()
            .then(response => <WorkflowListItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowItems(versionId: number) : Promise<any[]> {
        return this.http.get(`services/workflow/items/${versionId}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowObjectTypes(changeType: WorkflowChangeType): Promise<WorkflowObjectType[]> {
        if (changeType == null || <any>changeType == '')
            return Promise.resolve([]);

        return this.http.get(`services/workflow/objecttypes?changeType=${changeType}`)
            .toPromise()
            .then(response => <WorkflowObjectType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowFieldTypes(id: number, type: string, allowHtml: boolean = false, additionalFields: string = ""): Promise<FieldType[]> {
        if (id == null || type == null)
            return Promise.resolve([]);
        return this.http.get(`services/workflow/fieldtypes/${type}/${id}?allowHtml=${allowHtml}&additionalFields=${additionalFields}`)
            .toPromise()
            .then(response => <FieldType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowItemDetails(typeId: number, itemId: number): Promise<any[]> {
        return this.http.get(`services/workflow/item/details/${typeId}/${itemId}`)
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

    cloneWorkflowDiagramModel(id: number): Promise<number> {
        //returns workflowtype newly created id
        return this.http.post('services/workflow/diagram/clone', {ID:id})
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

    hasPendingWorkflowItems(id: number): Promise<boolean> {
        return this.http.get(`services/workflow/type/${id}/haspendingitems`)
            .toPromise()
            .then(response => <boolean>response.json())
            .catch(err => this.handleError(err));
    }

    deleteWorkflowType(id: number): Promise<number> {
        return this.http.delete(`services/workflow/type/${id}/delete`)
            .toPromise()
            .then(response => <number>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowTypeModel(id: number): Promise<WorkflowDiagramModel> {
        if (id == null || id < 1)
            return Promise.resolve(null);
        return this.http.get(`services/workflow/type/${id}`)
            .toPromise()
            .then(response => <WorkflowDiagramModel>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowDetailsV2(id: number): Promise<any> {
        return this.http.get(`services/workflow/item/detail/${id}`)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowVersions(id: number): Promise<any[]> {
        return this.http.get(`services/workflow/type/${id}/versions`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAssignedWorkflowInstancesByTypeId(id: number, resourceId: number,version:number,stepId:number): Promise<any> {        
        let url = `services/workflow/type/${id}/myinstances`;
        if (resourceId && !isNaN(resourceId)) {
            url += `?resourceId=${resourceId}`;
            url += `&version=${version}`;
            url += `&stepId=${stepId}`;
        }
        else {
            url += `?version=${version}`;
            url += `&stepId=${stepId}`;
        }
        return this.http.get(url)
                .toPromise()
                .then(response => <any>response.json())
                .catch(err => this.handleError(err));        
    }

    getAssignedWorkflowInstancesSummary(id: number, resourceId: number, version: number, stepId: number): Promise<any> {
        let url = `services/workflow/type/${id}/myinstances/summary`;
        if (resourceId && !isNaN(resourceId)) {
            url += `?resourceId=${resourceId}`;
            url += `&version=${version}`;
            url += `&stepId=${stepId}`;
        }
        else {
            url += `?version=${version}`;
            url += `&stepId=${stepId}`;
        }
        return this.http.get(url)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowProcedures(): Promise<WorkflowTaskProcedure[]> {
        return this.http.get('services/workflow/procedures')
            .toPromise()
            .then(response => <WorkflowTaskProcedure[]>response.json())
            .catch(err => this.handleError(err));
    }

    getEmailTaskRecipientType(): Promise<EmailTaskRecipientTypeInfo[]> {
        return this.http.get('services/workflow/emailtaskrecipienttypes')
            .toPromise()
            .then(response => <EmailTaskRecipientTypeInfo[]>response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowsByTypeList(types: string, filteredObject?: string, filteredObjectId?: number) {
        let uri = `services/workflow/typelist?types=${types}`;

        if (filteredObject != null && filteredObjectId != null) {
            uri += `&filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`;
        }

        return this.http.get(uri)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));

    }

    getWorkflowVersionStepHistory(id: number, filteredObject?: string, filteredObjectId?: number) {
        let uri = `services/workflow/versionstep/history/${id}`;

        if (filteredObject != null && filteredObjectId != null) {
            uri += `?filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`;
        }

        return this.http.get(uri)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));

    }

    getWorkflowVersionStepEvents(id: number) {
        return this.http.get(`services/workflow/versionstep/events/${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));

    }

    exportVersionStepHistory(id: number, filteredObject: string = null, filteredObjectId: number = null) {
        
        let uri = `services/workflow/versionstep/history/${id}/excel.xls`;

        if (filteredObject != null && filteredObjectId != null) {
            uri += `?filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`;
        }


        this.http.get(uri, { responseType: ResponseContentType.Blob }).subscribe((data: Response) => this.downloadFile(data, 'excel.xlsx'));  
    }

    downloadFile(data: Response, filename: string) {
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data.blob(), filename);
        }
        else {
            var url = window.URL.createObjectURL(data.blob());
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    getWorkflowOpenActions(types: string) {
        return this.http.get(`services/workflow/openactions?types=${types}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowVersionStepFormLookups(object: string, objectId: number) {
        return this.http.get(`services/workflow/versionstep/form/lookups/${object}/${objectId}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getIssueObjectSuggestions(phrase: string) {
        return this.http.get(`api/tagsuggestions?phrase=${phrase}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getAllowIntersectTypes(object: string, objectId: number) {
        return this.http.get(`api/${object}/${objectId}/relationshiptypes`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getIntersectType(id: number) {
        return this.http.get(`api/v2/relationships/types/${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getReferenceItemsForField(fieldId: number) {
        return this.http.get(`api/referenceItems/field/${fieldId}/items.json`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getIssueTypeAllocations(issueTypeId: number) {
        return this.http.get(`api/issuetype/${issueTypeId}/allocations`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postIssueTypeAllocation(item: any) {
        let values: any = {};

        //takes the form and convert any array values to , separated string values
        for (var p in item) {
            if (item.hasOwnProperty(p)) {
                if (Array.isArray(item[p])) {
                    values[p] = item[p].join();
                }
                else {
                    values[p] = item[p];
                }
            }
        }
        return this.postDynamic(this.http, 'IssueTypeRelation', values);
    }

    deleteIssueTypeAllocation(issueTypeId: number, assetTypeId: number) {
        return this.http.delete(`form/DeleteIssueTypeRelation?issueTypeID=${issueTypeId}&assetTypeID=${assetTypeId}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    clearLastExecutionDate(id: number) {
        return this.http.delete(`services/workflow/lastexecution/${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getWorkflowItemSteps(itemId: number): Promise<WorkflowItemStep[]> {
        return this.http.get(`services/workflow/item/${itemId}`)
            .toPromise()
            .then(response => <WorkflowItemStep[]>response.json())
            .catch(err => this.handleError(err));
    }

    exportItemSteps(itemId: number) {
        this.http.get(`services/workflow/item/${itemId}/excel/excel.xls`, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, "Workflow Steps.xlsx"));
    }

    getWorkflowStepDetail(itemStepId: number) {
        return this.http.get(`services/workflow/step/detail/${itemStepId}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postWorkflowBulkReassign(model: BulkWorkflowReassignModel) {
        return this.http.post('services/workflow/ReassignWorkflowResource/bulk', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}