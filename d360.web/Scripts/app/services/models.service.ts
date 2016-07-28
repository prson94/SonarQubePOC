///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Model } from '../models/model.model';

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
}