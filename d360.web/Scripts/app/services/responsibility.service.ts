import { Injectable } from '@angular/core';
import { FormHelper, SelectItem } from '../models/form.model';
import { ResponsibilityEditorModel, ResponsibilityItem, ResponsibilityItemDetail, IResponsibilityService, ResponsibilityItemDetailV2, ResponsibilityOverrideDeleteModel, ResponsibilityOverridePostModel } from '../models/responsibility.model';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';


@Injectable({
    providedIn: 'root'
})
export class ResponsibilityService extends BaseObservableService implements IResponsibilityService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getResponsibilityDetail(assetUid: string): Observable<ResponsibilityItemDetailV2[]> {
        return this.http.get(`/api/v2/responsibilities/assignments/${assetUid}`)
            .pipe(
                map((response) => <ResponsibilityItemDetailV2[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getHasResponsibilities(assetUid: string): Observable<boolean> {
        return this.http.get(`/api/v2/responsibilities/hasassignments/${assetUid}`)
            .pipe(
                map((response) => <boolean>response),
                catchError((err) => this.handleError(err))
            );
    }

    getResponsibilityItemEditor(assetID: number, responsibilityID: number, assetUid: string, responsibilityUid: string, resourceUid: string): Observable<ResponsibilityEditorModel> {
        return this.http.get(`form/Responsibility?assetID=${assetID}&overrideID=${responsibilityID}&assetUid=${assetUid}&responsibilityUid=${responsibilityUid}&resourceUid=${resourceUid}`)

            .pipe(
                map((response) => <ResponsibilityEditorModel>response),
                map((model) => {
                    FormHelper.mapSelectItems(model.resources);
                    FormHelper.mapSelectItems(model.responsibilityTypes);

                    if (model.responsibility.SecurityAsset)                        
                        model.selectedResource = model.responsibility.SecurityAsset + '|' + model.responsibility.SecurityAssetID;                    

                    if (model.responsibility.ResponsibilityTypeID) {
                        model.selectedResponsibilityType = model.responsibilityTypes.find((x) => x.Selected === true).Value;                       
                    }

                    return model;
                }),
                catchError(err => this.handleError(err))
            );
    }

    postResponsibility(assetUid: string, responsibilityUid: string, responsibilityOverridePostModel: ResponsibilityOverridePostModel): Observable<JsonResult> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })            
        };

        return this.http.post(`/api/v2/responsibilities/${assetUid}/${responsibilityUid}`, JSON.stringify(responsibilityOverridePostModel) , httpOptions)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    putResponsibility(responsibility: ResponsibilityItem): Observable<JsonResult> {
        var headers = new HttpHeaders({ 'Content-Type': 'application/json' })
        return this.http.put('form/responsibility', JSON.stringify(responsibility), { headers })
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteResponsibility(assetUid: string, responsibilityUid: string, resourceUid: string): Observable<JsonResult> {
        var responsibilityOverrideDeleteModel: ResponsibilityOverrideDeleteModel = new ResponsibilityOverrideDeleteModel();
        responsibilityOverrideDeleteModel.ResourceUid = resourceUid;      

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: [responsibilityOverrideDeleteModel]
        };

        return this.http.delete(`/api/v2/responsibilities/${assetUid}/${responsibilityUid}`, httpOptions)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }   
}