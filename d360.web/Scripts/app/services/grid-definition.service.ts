import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { GridDefinition } from '../models/grid-definition.model';
//import { FieldFilterModel } from '../models/filter-field.model';

@Injectable()
export class GridDefinitionService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getGridDefinition(objectID: number, objectType: string, parentID?: number, parentType?: string): Promise<GridDefinition> {
        let url = `api/${objectType}/${objectID}/grid/definition`;

        if (parentID && parentType) {
            url += `?${parentType}=${parentID}`;
        }
        
        return this.http.get(url)
            .toPromise()
            .then(response => <GridDefinition>response.json())
            .catch(err => this.handleError(err));
    }

    //getFieldFiltersByType(objectType: string, objectId: number): Promise<FieldFilterModel[]> {
    //    return this.http.get(`/api/${objectType}/${objectId}/fieldfilters`)
    //        .toPromise()
    //        .then(response => <FieldFilterModel[]>response.json())
    //        .catch(err => this.handleError(err));
    //}
}