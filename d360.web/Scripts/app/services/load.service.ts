import {Injectable} from '@angular/core';
import {LoadDetail, LoadFilePostModel, LoadColumn} from '../models/load.model';
import {MessagesObservableService} from './messages-observable.service';
import {GridColumn} from '../models/grid-definition.model';
import {SelectItem} from 'primeng/api';
import {JsonResult} from '../models/jsonresult.model';
import {BaseObservableService} from "./baseObservable.service";
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

declare var CompanySettings: any;

@Injectable({
    providedIn: 'root'
})
export class LoadService extends BaseObservableService {

    lineageVersion: number = 1;
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

    getActionOptions(): SelectItem[] {
        this.aOptions = [
            {label: 'Promotion', value: 'P'},
            {label: 'Relation', value: 'R'},
            {label: 'Responsibilities', value: 'O'},
            {label: 'Unrelation', value: 'U'},
            {label: 'Users/Groups', value: 'M'}
        ];
        if (CompanySettings != null && CompanySettings.LineageVersion != null) {
            this.lineageVersion = CompanySettings.LineageVersion;
        }
        return this.aOptions;
    }

    getTypeOptions(action: string): Observable<SelectItem[]> {
        return this.http.get(`/form/Load_TypeOptions?act=${action}`)
            .pipe(
                map(response => {
                    let i = [];

                    response["forEach"](r => {
                        i.push({label: r.title, value: r.value});
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
