import { Injectable } from '@angular/core';
import { LoadDetail, LoadFilePostModel, LoadColumn, LoadItemsModel } from '../models/load.model';
import { MessagesObservableService } from './messages-observable.service';
import { GridColumn } from '../models/grid-definition.model';
import { SelectItem } from 'primeng/api';
import { JsonResult } from '../models/jsonresult.model';
import { BaseObservableService } from "./baseObservable.service";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

@Injectable({
    providedIn: 'root'
})
export class LoadService extends BaseObservableService {

    aOptions: any[] = [];

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getLoads(): Observable<LoadDetail[]> {
        return this.http.get('api/loads').pipe(
            map(response => <LoadDetail[]>response),
            catchError(err => this.handleError(err))
        );
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

    getLoadItemsV2(loadUid: string, params: any): Observable<LoadItemsModel> {
        var qString = '';
        if (params) {
            qString = Object.keys(params).map((key) => key + '=' + params[key]).join('&');
            if (qString) {
                qString = '?' + qString;
            }
        }

        var url = `api/v2/executions/bulkload/${loadUid}/items${qString}`;
        return this.http.get(url).pipe(
            map((response) => <LoadItemsModel>response),
            catchError((err) => this.handleError(err))
        );
    }

    getLoadUid(id: number): Observable<any> {
        return this.http.get(`api/loads/${id}/uid`).pipe(
            map((response) => <any>response),
            catchError((err) => this.handleError(err))
        );
    }

    getActionOptions(): SelectItem[] {
        this.aOptions = [
            { label: $localize`Promotion`, value: 'P' },
            { label: $localize`Relation`, value: 'R' },
            { label: $localize`Responsibilities`, value: 'O' },
            { label: $localize`Unrelation`, value: 'U' },
            { label: $localize`Users/Groups`, value: 'M' }
        ];
        return this.aOptions;
    }

    getTypeOptions(action: string): Observable<SelectItem[]> {
        return this.http.get(`/form/Load_TypeOptions?act=${action}`)
            .pipe(
                map(response => {
                    let i = [];

                    response["forEach"](r => {
                        i.push({ label: r.title, value: r.value });
                    });

                    return <SelectItem[]>i;
                }),
                catchError(err => this.handleError(err))
            );
    }

    getExpectedColumns(action: string, type: string, id: number): Observable<LoadColumn[]> {
        return this.http.get(`form/Load_ExpectedColumns?action=${action}&id=${id}&type=${type}`).pipe(
            map(response => <LoadColumn[]>response),
            catchError(err => this.handleError(err))
        );
    }

    getExpectedColumnsExcel(action: string, type: string, id: number): Observable<LoadColumn[]> {
        return this.http.get(`form/Load_ExpectedColumns_ToExcel?action=${action}&id=${id}&type=${type}`).pipe(
            map(response => <LoadColumn[]>response),
            catchError(err => this.handleError(err))
        );
    }

    getLoadErrorsXls(id: number) {
        window.location.assign(`/form/loads/${id}/Errors.xlsx`);
    }

    getLoadOriginalXls(id: number) {
        window.location.assign(`/form/loads/${id}/all.xlsx`);
    }

    postLoad(model: LoadFilePostModel): Observable<JsonResult> {
        return this.http.post('form/AddLoad', model).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }
}
