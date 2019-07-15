import { Injectable } from '@angular/core';
import {HttpClient,HttpHeaders} from '@angular/common/http'
import { FormHelper, SelectItem } from '../models/form.model';
import { ResponsibilityEditorModel, ResponsibilityItem, ResponsibilityItemDetail, IResponsibilityService } from '../models/responsibility.model';
import { JsonResult } from '../models/jsonresult.model'
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { Observable } from 'rxjs';
import { catchError,map } from 'rxjs/operators';


@Injectable()
export class ResponsibilityService extends BaseObservableService implements IResponsibilityService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getResponsibilityDetail(assetID: number): Observable<ResponsibilityItemDetail[]> {
        return this.http.get(`api/${assetID}/ownership`)
            .pipe(
                map(response => <ResponsibilityItemDetail[]>response),
                catchError(err=>this.handleError(err))
            );
    }

    getResponsibilityItemEditor(assetID: number, responsibilityID: number): Observable<ResponsibilityEditorModel> {
        return this.http.get(`form/Responsibility?assetID=${assetID}&overrideID=${responsibilityID}`)

            .pipe(
                map(response => <ResponsibilityEditorModel>response),
                map(model => {
                    FormHelper.mapSelectItems(model.resources);
                    FormHelper.mapSelectItems(model.responsibilityTypes);

                    if (model.responsibility.SecurityAsset)
                        model.selectedResource = model.responsibility.SecurityAsset + '|' + model.responsibility.SecurityAssetID;

                    if (model.responsibility.ResponsibilityTypeID)
                        model.selectedResponsibilityType = model.responsibility.ResponsibilityTypeID.toString();

                    return model;
                }),
                catchError(err => this.handleError(err))
        );
    }

    postResponsibility(responsibility: ResponsibilityItem): Observable<JsonResult> {

        let headers = new HttpHeaders({
            'Content-Type': 'application/json' 
        });
        return this.http.post('form/responsibility', JSON.stringify(responsibility), { headers: headers })
            .pipe(
                map(response => <JsonResult>response),
                catchError(err=>this.handleError(err))
            );
    }  

    putResponsibility(responsibility: ResponsibilityItem): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        return this.http.put('form/responsibility', JSON.stringify(responsibility), { headers: headers })
            .pipe(
                map(response => response),
                catchError(err=>this.handleError(err))
            );
    }

}