import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { GridDefinition } from '../models/grid-definition.model';

@Injectable()
export class GridDefinitionService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getGridDefinition(objectID: number, objectType: string, parentID?: number, parentType?: string): Promise<GridDefinition> {
        let url = `api/${objectType}/${objectID}/grid/definition`;

        if ((parentID >= 0) && parentType) {
            url += `?target=${parentType}&targetID=${parentID}`;
        }
        
        return this.http.get(url)
            .toPromise()
            .then(response => <GridDefinition>response.json())
            .catch(err => this.handleError(err));
    }    
}