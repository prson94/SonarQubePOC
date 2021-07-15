import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
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
    WorkflowTypeItem,
    ActionEditorModel,
    AllocationAPIModel,
    AllocationRequestModel,
    WorkflowReassignmentAssetType,
} from '../models/workflow.model';
import { FieldType } from '../models/fields.model';
import { SelectItem, FormHelper } from '../models/form.model';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';
import { DynamicGridResultsInData } from '../models/grid-definition.model';
import { Observable,of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiResult, ErrorResponse } from '../models/apiresult.model';
import { AssetTypeClass } from '../models/asset.model';

@Injectable({
    providedIn: 'root'
})
export class WorkflowService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService);}
    
    getMyCounts(daysToLookBack: number, resourceId?: number) : Observable<Count[]> {
        return this.http.get(`api/count/assignments/${daysToLookBack}` + (resourceId ? `?id=${resourceId}` : ''))
            .pipe(
                map(response => <Count[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    exportAllIssueDetails(all?: boolean) {
        window.location.assign(`services/workflow/all/issues/excel/excel.xls?all=${(all === undefined || all) ? 'true':'false'}`);        
    }

    getAllIssueDetails(all?: boolean): Observable<IssueDetail[]> {
        let url = `services/workflow/${(all === undefined || all) ? 'all':'my'}/issues?$orderby=DateStarted%20desc,Issue`;
                
        return this.http.get(url)
            .pipe(
                map(response => <IssueDetail[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    getIssues(objectID: number, objectType: string): Observable<Issue[]> {
        let url = 'services/workflow/issue/type/';

        if (objectID > 0 && objectType != undefined) {
            url += `${objectID}/${objectType}`;
        }

        return this.http.get(url)
            .pipe(
                map(response => <Issue[]>response),
                catchError(err=>this.handleError(err))
            );
    }
    
    updateIssue(issue: Issue, action: string, comment: string, assignTo?: string): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        return this.http
            .post(`/services/workflow/tasks/${issue.WorkflowID}`, JSON.stringify({ WorkflowAction: action, AssignTo: assignTo, Comment: comment }), { headers: headers })
            .pipe(
            map(response => <JsonResult>response),
                catchError(err=>this.handleError(err))
            );
    }
  
    raiseIssues(actionTypeUid: string, action: any): Observable<ApiResult & ErrorResponse> {
        let actionArray: ActionEditorModel[] = [];
        actionArray.push(action);

        return this.http.post(`/api/v2/actions/${actionTypeUid}?lookupFieldsPassedByValue=true`, actionArray)
            .pipe(
                map(response => <ApiResult & ErrorResponse> response[0]),
                catchError(err => this.handleError(err))
            );  
    }
   
    getWorkflowIssueTypes(object: string = null, objectId: number = null, params: any): Observable<WorkflowIssueType[]> {
        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }

        return this.http.get(`/api/v2/actions/types${qString}`)
            .pipe(
                map(response => <WorkflowIssueType[]>response),
                catchError(err=>this.handleError(err))
            ); 
    }

    getAdminWorkflowIssueTypes(): Observable<WorkflowIssueType[]> {
        return this.http.get('/api/v2/actions/types')
            .pipe(
                map(response => <WorkflowIssueType[]>response),
                catchError(err=>this.handleError(err))
            );  
    }

    getIssueByUID(uid: string): Observable<WorkflowIssueType> {
        return this.http.get(`api/IssueType/${uid}`)
            .pipe(
                map(response => <WorkflowIssueType>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteWorkflowIssueType(actionTypeUid: string): Observable<ApiResult & ErrorResponse> {

        var model = { cascade: false };
        var queryString = '?_RequestfromUI=true';

        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: model
        };

        return this.http.delete(`/api/v2/actions/type/${actionTypeUid}` + queryString, httpHeaders)
            .pipe(
                map(response => <ApiResult & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );
    }

    saveIssueType(issueType: WorkflowIssueType): Observable<ApiResult & ErrorResponse> {
        var model = { Uid: issueType.Uid, Name: issueType.Name, Description: issueType.Description };
        if (issueType.Uid == undefined || !issueType.Uid) {            
            return this.http.post(`/api/v2/actions/type/`, model)
                .pipe(
                    map(response => <ApiResult & ErrorResponse>response),
                    catchError(err => this.handleError(err))
                );  
        }
        return this.http.put(`/api/v2/actions/type/`, model)
            .pipe(
                map(response => <ApiResult & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );  
    }

    //#region diagram

    public getWorkflowDiagram(id: number,uid:string, version?: number, filteredObject?: string, filteredObjectId?: number): Observable<WorkflowDiagramModel> {
        let uri = `services/workflow/diagram/${id}/${uid}${version != null ? '?version=' + version : ''}`

        if (filteredObject != null && filteredObjectId != null)
            uri += `${version == null ? '?' : '&'}filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`

        return this.http.get(uri)
            .pipe(
                map(response => <WorkflowDiagramModel>response),
                catchError(err=> this.handleError(err))
            );
    }

    //#endregion


    //#region bulk

    getWorkflowBulkForm(model: BulkWorkflowFormModel):Observable<any> {
        return this.http.post('/services/workflow/form/bulk', model)
            .pipe(
                map(response => response),
                catchError(err=>this.handleError(err))
            );
    }

    submitBulkWorkflowForm(model: BulkWorkflowFormModel) : Observable<any>{
        return this.http.post('/services/workflow/SubmitWorkflowForm/bulk', model)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
               );
    }

    //#endregion

    getWorkflowForm(id: number, itemStepId: number): Observable<WorkflowForm> {
        return this.http.get(`/services/workflow/form/${id}/${itemStepId}`)
            .pipe(
                map(response => <WorkflowForm>response),
                catchError(err=>this.handleError(err))
            );
    }
 
    reassignUser(itemStepId: number, resourceId: number, clearAssignents: boolean): Observable<JsonResult> {
        return this.http
            .post(`services/workflow/ReassignWorkflowResource/${itemStepId}/${resourceId}/${clearAssignents}`, null)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err=>this.handleError(err))
            );
    }

    reassignObject(itemId: number, workflowId: number, objectId: number, objectType: string, stepId: number, resourceId: number = null): Observable<JsonResult> {
        let url = `services/workflow/ReassignWorkflowObject/${itemId}/${workflowId}/${objectId}/${objectType}/${stepId}`;

        if (!isNaN(resourceId))
            url += `?resourceId=${resourceId}`;

        return this.http
            .post(url, null)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err=>this.handleError(err))
            );
    }

    submitWorkflowForm(itemId: number, stepId: number, fields: any[]): Observable<any> {
        return this.http
            .post(`services/workflow/SubmitWorkflowForm/${itemId}/${stepId}`, fields)
            .pipe(
                map(response => response),
                catchError(err=>this.handleError(err))
            );
    }

    getActivityTypes(): Observable<ActivityTypeInfo[]> {
        return this.http.get('services/workflow/activitytypes')
            .pipe(
                map(response => <ActivityTypeInfo[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getChangeTypes(): Observable<ChangeTypeInfo[]> {
         return this.http.get('services/workflow/changetypes')
            .pipe(
                map(response => <ChangeTypeInfo[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    getTransitionTypes(): Observable<TransitionTypeInfo[]> {
        return this.http.get('services/workflow/transitiontypes')
            .pipe(
                map(response => <TransitionTypeInfo[]>response),
                catchError(err => this.handleError(err))
             );
    }

    getAdminTypes(): Observable<WorkflowTypeItem[]> {
        return this.http.get('api/v2/workflow/types')
            .pipe(
            map(response => <WorkflowTypeItem[]> response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowTypeId(uid: string):Observable<number> {
        return this.http.get(`api/v2/workflow/type/${uid}/id`)
            .pipe(
                map(response => <number>response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowId(uid: string): Observable<number> {
        return this.http.get(`api/v2/workflow/${uid}/legacyData`)
            .pipe(
                map(response => <number>response),
                catchError(err => this.handleError(err))
            );
    }


    getTypes(): Observable<any> {
        return this.http.get('services/workflow/types')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getObjectTypes(objectID: number, objectType: string): Observable<WorkflowListItem[]> {
        return this.http.get(`services/workflow/types/${objectID}/${objectType}`)
            .pipe(
                map(response => <WorkflowListItem[]> response),
                catchError(err => this.handleError(err))
            );
    }

    getScoreTypes(objectID: number, objectType: string) {
        return this.http.get(`services/workflow/scoretypes/${objectID}/${objectType}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowItems(versionId: number) : Observable<any[]> {
        return this.http.get(`services/workflow/items/${versionId}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowObjectTypes(changeType: WorkflowChangeType): Observable<WorkflowObjectType[]> {
        if (changeType == null || <any>changeType == '')
            return of([]);

        return this.http.get(`services/workflow/objecttypes?changeType=${changeType}`)
            .pipe(
            map(response => <WorkflowObjectType[]>response),
                catchError(err => this.handleError(err))
            );
            
    }

    getWorkflowFieldTypes(id: number, type: string, allowHtml: boolean = false, additionalFields: string = ""): Observable<FieldType[]> {
          if (id == null || type == null)
            return of([]);
        return this.http.get(`services/workflow/fieldtypes/${type}/${id}?allowHtml=${allowHtml}&additionalFields=${additionalFields}`)
            .pipe(
                map(response => <FieldType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowItemDetails(typeId: number, itemId: number): Observable<any[]> {
        return this.http.get(`services/workflow/item/details/${typeId}/${itemId}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    saveWorkflowDiagramModel(model: WorkflowDiagramModel): Observable<number> {
        //returns workflowtype id
        return this.http.post('services/workflow/diagram/save', model)
            .pipe(
                map(response => <number>response),
                catchError(err => this.handleError(err))
            );
    }

    cloneWorkflowDiagramModel(uid: string): Observable<string> {
        //returns workflowtype newly created id
        return this.http.post('services/workflow/diagram/clone', { UID:uid })
            .pipe(
                map(response => <string>response),
                catchError(err => this.handleError(err))
            );
    }

    getLookupList(id: number): Observable<any[]> {
        return this.http.get(`api/lookup/list/${id}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    hasPendingWorkflowItems(id: number): Observable<boolean> {
        return this.http.get(`services/workflow/type/${id}/haspendingitems`)
            .pipe(
                map(response => <boolean>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteWorkflowType(id: number, uid: string): Observable<number> {
        return this.http.delete(`services/workflow/type/${id}/${uid}/delete`)
            .pipe(
                map(response => <number>response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowTypeModel(id: number,uid:string): Observable<WorkflowDiagramModel> {
        if ((id == null || id < 1) && (uid==null  || uid == "00000000-0000-0000-0000-000000000000")) 
            return of(null);
        return this.http.get(`services/workflow/type/${id}/${uid}`)
            .pipe(
            map(response => <WorkflowDiagramModel>response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowDetailsV2(id: number): Observable<any> {
        return this.http.get(`services/workflow/item/detail/${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
           
    }

    getWorkflowVersions(id: number): Observable<any[]> {
        return this.http.get(`services/workflow/type/${id}/versions`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getAssignedWorkflowInstancesByTypeId(id: number, resourceId: number,version:number,stepId:number): Observable<any> {        
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
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getAssignedWorkflowInstancesSummary(id: number, resourceId: number, version: number, stepId: number): Observable<any> {
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
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowProcedures(): Observable<WorkflowTaskProcedure[]> {
        return this.http.get('services/workflow/procedures')
            .pipe(
            map(response => <WorkflowTaskProcedure[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getEmailTaskRecipientType(): Observable<EmailTaskRecipientTypeInfo[]> {
        return this.http.get('services/workflow/emailtaskrecipienttypes')
            .pipe(
            map(response => <EmailTaskRecipientTypeInfo[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowsByTypeList(types: string, filteredObject?: string, filteredObjectId?: number):Observable<any> {
        let uri = `services/workflow/typelist?types=${types}`;

        if (filteredObject != null && filteredObjectId != null) {
            uri += `&filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`;
        }

        return this.http.get(uri)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );

    }

    getWorkflowVersionStepHistory(id: number, filteredObject?: string, filteredObjectId?: number):Observable<any> {
        let uri = `services/workflow/versionstep/history/${id}`;

        if (filteredObject != null && filteredObjectId != null) {
            uri += `?filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`;
        }

        return this.http.get(uri)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );

    }

    getWorkflowVersionStepEvents(id: number):Observable<any> {
        return this.http.get(`services/workflow/versionstep/events/${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    exportVersionStepHistory(id: number, filteredObject: string = null, filteredObjectId: number = null) {
        
        let uri = `services/workflow/versionstep/history/${id}/excel.xls`;

        if (filteredObject != null && filteredObjectId != null) {
            uri += `?filteredObject=${filteredObject}&filteredObjectId=${filteredObjectId}`;
        }


        this.http.get(uri, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'excel.xlsx'));  
    }

    downloadFile(data: Blob, filename: string) {
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    getWorkflowOpenActions(types: string):Observable<any> {
        return this.http.get(`services/workflow/openactions?types=${types}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowVersionStepFormLookups(object: string, objectId: number, issueObject: string = null, issueObjectId: number = null): Observable<any> {
        let query = ``;

        if (issueObject != null) {
            query = `?issueObject=${issueObject}&issueObjectId=${issueObjectId}`;
        }

        return this.http.get(`services/workflow/versionstep/form/lookups/${object}/${objectId}${query}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getIssueObjectSuggestions(phrase: string): Observable<any> {
        return this.http.get(`api/tagsuggestions?phrase=${phrase}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getAllowIntersectTypes(object: string, objectId: number) : Observable<any>{
        return this.http.get(`api/${object}/${objectId}/relationshiptypes`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getIntersectType(id: number) : Observable<any>{
        return this.http.get(`api/v2/relationships/types/${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getReferenceItemsForField(fieldId: number): Observable<any> {
        return this.http.get(`api/referenceItems/field/${fieldId}/items.json`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getIssueTypeAllocations(actionTypeUid: string): Observable<AllocationAPIModel[]>{        
        return this.http.get(`api/v2/actions/allocations/${actionTypeUid}`)
            .pipe(
                map(response => {
                    let r = <AllocationAPIModel[]>response;                    
                    r.forEach(x => x.ClassName = AssetTypeClass[x.Class])
                    return r;
                }),
                catchError(err => this.handleError(err))
            );
    }

    postIssueTypeAllocation(actionTypeUid: string, allocation: AllocationRequestModel): Observable<JsonResult> { 
        return this.http.post(`/api/v2/actions/allocation/${actionTypeUid}`, allocation)
            .pipe(
                map(response => <ApiResult & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );  
    }

    deleteIssueTypeAllocation(actionTypeUid: string, assetTypeUid: string): Observable<JsonResult> {
        
        return this.http.delete(`/api/v2/actions/allocations/${actionTypeUid}/${assetTypeUid}`)
            .pipe(
                map(response => <ErrorResponse>response),
                catchError(err => this.handleError(err))
        );
    }

    clearLastExecutionDate(id: number, uid: string):Observable<any> {
        return this.http.delete(`services/workflow/lastexecution/${id}/${uid}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowItemSteps(itemId: number): Observable<WorkflowItemStep[]> {
        return this.http.get(`services/workflow/item/${itemId}`)
            .pipe(
            map(response => <WorkflowItemStep[]>response),
                catchError(err => this.handleError(err))
            );
    }

    exportItemSteps(itemId: number) {
        this.http.get(`services/workflow/item/${itemId}/excel/excel.xls`, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, "Workflow Steps.xlsx"));
    }

    getWorkflowStepDetail(itemStepId: number):Observable<any> {
        return this.http.get(`services/workflow/step/detail/${itemStepId}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    postWorkflowBulkReassign(model: BulkWorkflowReassignModel):Observable<any> {
        return this.http.post('services/workflow/ReassignWorkflowResource/bulk', model)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowReassignmentAssetTypes(workflowItemId: number): Observable<WorkflowReassignmentAssetType[]> {
        return this.http.get(`api/v2/workflow/${workflowItemId}/reassignment/object/types`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getWorkflowReassignmentAssets(assetTypeId: number, query: string): Observable<WorkflowReassignmentAssetType[]> {
        return this.http.get(`api/v2/workflow/reassignment/objects/${assetTypeId}?query=${query}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }
}