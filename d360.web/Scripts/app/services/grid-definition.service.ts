import {Injectable} from '@angular/core';
import {MessagesObservableService} from './messages-observable.service';
import {GridDefinition} from '../models/grid-definition.model';
import {BaseObservableService} from "./baseObservable.service";
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

@Injectable()
export class GridDefinitionService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getGridDefinition(
        objectID: number,
        objectType: string,
        parentID?: number,
        parentType?: string
    ): Observable<GridDefinition> {
        let url = `api/${objectType}/${objectID}/grid/definition`;

        if ((parentID >= 0) && parentType) {
            url += `?target=${parentType}&targetID=${parentID}`;
        }

        return this.http.get(url).pipe(
            map(response => <GridDefinition>response),
            catchError(err => this.handleError(err))
        );
    }
}
