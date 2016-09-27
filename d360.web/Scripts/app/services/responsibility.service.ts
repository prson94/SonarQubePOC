
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { FormHelper, SelectItem } from '../models/form.model';
import { ResponsibilityEditorModel, ResponsibilityItem, ResponsibilityContextItem, IResponsibilityService } from '../models/responsibility.model';
import { MessagesService } from './index';
import { BaseService } from './base.service';

@Injectable()
export class ResponsibilityService extends BaseService implements IResponsibilityService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResponsibilityDetail(objectID: number, objectType: string, showHidden: boolean = true): Promise<ResponsibilityItem[]> {
        return this.http.get(`api/${objectType}/${objectID}/ownership?showHidden=${showHidden}`)
            .toPromise()
            .then(response => <ResponsibilityItem[]>response.json())
            .then(r => {
                //TODO: use same model in api get as post instead of Responsibility vs ResponsibilityDetail???
                r.forEach(i => i.ID = i.ResponsibilityID);
                return r;
            })
            .catch(err=>this.handleError(err));
    }

    getResponsibilityItemEditor(objectID: number, objectType: string, responsibilityID: number): Promise<ResponsibilityEditorModel> {
        return this.http.get(`form/Responsibility?responsibilityID=${responsibilityID}&id=${objectID}&type=${objectType}`)
            .toPromise()
            .then(response => <ResponsibilityEditorModel>response.json())
            .then(model => {
                FormHelper.mapSelectItems(model.resources);
                FormHelper.mapSelectItems(model.responsibilityTypes);
                FormHelper.mapSelectItems(model.contexts);

                if (model.responsibility.ResponsibleObjectType)
                    model.selectedResource = model.responsibility.ResponsibleObjectType + '|' + model.responsibility.ResponsibleObjectID;
                else if (model.resources && model.resources.length > 0)
                    model.selectedResource = model.resources[0].value;

                if (model.responsibility.ResponsibilityTypeID)
                    model.selectedResponsibilityType = model.responsibility.ResponsibilityTypeID.toString();
                else if (model.responsibilityTypes && model.responsibilityTypes.length > 0)
                    model.selectedResponsibilityType = model.responsibilityTypes[0].value;

                model.selectedContexts = model.contexts.filter(c => c.Selected).map(c => c.value);

                return model;

            })
            .catch(err=>this.handleError(err));
    }

    postResponsibility(responsibility: ResponsibilityItem): Promise<any> {
        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        return this.http.post('form/responsibility', JSON.stringify(responsibility), { headers: headers })
            .toPromise()
            .catch(err=>this.handleError(err));
    }    
}