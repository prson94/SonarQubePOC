///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { GridDefinition } from '../models/grid-definition.model';

@Injectable()
export class GridDefinitionService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getGridDefinition(objectID: number, objectType: string): Promise<GridDefinition> {
        return this.http.get(`api/${objectType}/${objectID}/grid/definition`)
            .toPromise()
            .then(response => <GridDefinition>response.json())
            .catch(err => this.handleError(err));
    }
}