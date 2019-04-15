import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {GridColumn} from '../../../../models/grid-definition.model';

import {MessagesService} from '../../../../services/messages.service';
import {BaseObservableService} from "../../../../services/baseObservable.service";

@Injectable()
export class BulkLoadItemService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getLoadColumns(id: number): Observable<GridColumn[]> {
        return this.http.get(`api/loads/${id}/columns`).pipe(
            map(response => <GridColumn[]>response),
            catchError(err => this.handleError(err))
        );
    }

    getLoadItems(id: number): Observable<any[]> {
        return this.http.get(`api/loads/${id}/items`).pipe(
            map(response => <any[]>response),
            catchError(err => this.handleError(err))
        );
    }

    getLoadErrorsXls(id: number) {
        window.location.assign(`/form/loads/${id}/Errors.xlsx`);
    }

    getLoadOriginalXls(id: number) {
        window.location.assign(`/form/loads/${id}/all.xlsx`);
    }
}
