import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {LoadColumn} from '../../models/load.model';

import {MessagesService} from '../messages.service';
import {BaseObservableService} from "../baseObservable.service";

@Injectable()
export class ColumnsService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getExpectedColumns(
        action: string,
        type: string,
        id: number
    ): Observable<LoadColumn[]> {
        return this.http.get(`form/Load_ExpectedColumns?action=${action}&id=${id}&type=${type}`).pipe(
            map(response => <LoadColumn[]>response),
            catchError(err => this.handleError(err))
        );
    }

    /* FIXME: never called */
    getExpectedColumnsExcel(
        action: string,
        type: string,
        id: number
    ): Observable<LoadColumn[]> {
        return this.http.get(`form/Load_ExpectedColumns_ToExcel?action=${action}&id=${id}&type=${type}`).pipe(
            map(response => <LoadColumn[]>response),
            catchError(err => this.handleError(err))
        );
    }
}
