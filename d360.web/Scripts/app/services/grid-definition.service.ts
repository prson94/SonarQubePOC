import {Injectable} from '@angular/core';
import {MessagesObservableService} from './messages-observable.service';
import {GridDefinition} from '../models/grid-definition.model';
import {BaseObservableService} from "./baseObservable.service";
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

@Injectable({
    providedIn: 'root'
})
export class GridDefinitionService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getGridDefinition(
        objectID: number | string,
        objectType: string,
        parentID?: number,
        parentType?: string,
        params: any = null
    ): Observable<GridDefinition> {
        let url = `api/${objectType}/${objectID}/grid/definition`;
        let addparameterchar = '?';

        if ((parentID >= 0) && parentType) {
            url += `?target=${parentType}&targetID=${parentID}`;
            addparameterchar = '&';
        }

        if (params) {
            var qString = Object.keys(params).map((key) => [key, params[key]].join("=")).join('&');
            url += addparameterchar + qString;
        }

        return this.http.get(url).pipe(
            map((response) => <GridDefinition>response),
            catchError(err => this.handleError(err))
        );
    }
}
