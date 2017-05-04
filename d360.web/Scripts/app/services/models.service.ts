import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Model, ModelHierarchy } from '../models/model.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class ModelsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getModels(): Promise<Model[]> {
        return this.http.get(`api/catalogs`)
            .toPromise()
            .then(response => <Model[]>response.json())
            .catch(err => this.handleError(err));
    }

    getModel(id: number): Promise<Model> {
        return this.http.get(`api/catalogs/${id}`)
            .toPromise()
            .then(response => <Model>response.json())
            .catch(err => this.handleError(err));
    }

    getModelHierarchy(id: number, details?: boolean, stripHtml: boolean = false): Promise<ModelHierarchy[]> {
        return this.http.get(`internal/taxonomy/ModelHierarchy${details ? 'Detailed': ''}?id=${id}${(details && stripHtml) ? '&stripHtml=true' : ''}`)
            .toPromise()
            .then(response => <ModelHierarchy[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteModelHierarchy(id: number): Promise<JsonResult>{
        return this.deleteDynamicWithResult(this.http, 'taxonomy', id);
    }

    saveModelHierarchy(hierarchy: ModelHierarchy): Promise<JsonResult> {
        if (hierarchy.ID == undefined || !hierarchy.ID) {
            return this.postDynamic(this.http, 'taxonomy', hierarchy);
        }
        return this.putDynamic(this.http, 'taxonomy', hierarchy);  
    }
}