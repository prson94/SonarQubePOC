import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import { Model, ModelHierarchy } from '../models/model.model';
import { JsonResult } from '../models/jsonresult.model';

import { MessagesService } from './messages.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class ModelsService extends BaseObservableService {
    constructor(
        private http: HttpClient, 
        messagesService: MessagesService
    ) { 
        super(messagesService); 
    }

    getModels(): Observable<Model[]> {
        return this.http.get('api/catalogs')
            .pipe(
                map(response => <Model[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getModel(id: number): Observable<Model> {
        return this.http.get(`api/catalogs/${id}`)
            .pipe(
                map(response => <Model>response),
                catchError(err => this.handleError(err))
            );
    }

    getModelHierarchy(
        id: number,
        details?: boolean,
        stripHtml: boolean = false
    ): Observable<ModelHierarchy[]> {
        const isStripHtml = (details && stripHtml) ? '&stripHtml=true' : '';
        const url = `internal/taxonomy/ModelHierarchy${details ? 'Detailed': ''}?id=${id}${isStripHtml}`;

        return this.http.get(url)
            .pipe(
                map(response => <ModelHierarchy[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteModelHierarchy(id: number): Observable<JsonResult>{
        return this.deleteDynamicWithResult(this.http, 'taxonomy', id);
    }

    saveModelHierarchy(hierarchy: ModelHierarchy): Observable<JsonResult> {
        if (hierarchy.ID == undefined || !hierarchy.ID) {
            return this.postDynamic(this.http, 'taxonomy', hierarchy);
        }

        return this.putDynamic(this.http, 'taxonomy', hierarchy);  
    }
}
