import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { FormHelper, SelectItem } from '../models/form.model';
import { ResponsibilityEditorModel, ResponsibilityItem, ResponsibilityItemDetail, IResponsibilityService } from '../models/responsibility.model';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { JsonResult } from '../models/jsonresult.model'

@Injectable()
export class ResponsibilityService extends BaseService implements IResponsibilityService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResponsibilityDetail(assetID: number): Promise<ResponsibilityItemDetail[]> {
        return this.http.get(`api/${assetID}/ownership`)
            .toPromise()
            .then(response => <ResponsibilityItemDetail[]>response.json())
            .catch(err=>this.handleError(err));
    }

    getResponsibilityItemEditor(assetID: number, responsibilityID: number): Promise<ResponsibilityEditorModel> {
        return this.http.get(`form/Responsibility?assetID=${assetID}&overrideID=${responsibilityID}`)
            .toPromise()
            .then(response => <ResponsibilityEditorModel>response.json())
            .then(model => {
                FormHelper.mapSelectItems(model.resources);
                FormHelper.mapSelectItems(model.responsibilityTypes);

                if (model.responsibility.SecurityAsset)
                    model.selectedResource = model.responsibility.SecurityAsset + '|' + model.responsibility.SecurityAssetID;
                else if (model.resources && model.resources.length > 0)
                    model.selectedResource = model.resources[0].value;

                if (model.responsibility.ResponsibilityTypeID)
                    model.selectedResponsibilityType = model.responsibility.ResponsibilityTypeID.toString();
                else if (model.responsibilityTypes && model.responsibilityTypes.length > 0)
                    model.selectedResponsibilityType = model.responsibilityTypes[0].value;

                return model;
            })
            .catch(err=>this.handleError(err));
    }

    postResponsibility(responsibility: ResponsibilityItem): Promise<JsonResult> {
        var headers = new Headers();
        headers.append('Content-Type', 'application/json');
        return this.http.post('form/responsibility', JSON.stringify(responsibility), { headers: headers })
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err=>this.handleError(err));
    }  

    putResponsibility(responsibility: ResponsibilityItem): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');
        return this.http.put('form/responsibility', JSON.stringify(responsibility), { headers: headers })
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

}