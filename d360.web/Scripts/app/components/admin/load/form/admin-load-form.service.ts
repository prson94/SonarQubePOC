import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";
import {SelectItem} from "primeng/api";

import {LoadColumn} from '../../../../models/load.model';
import {LoadFilePostModel} from "../../../../models/load.model";
import {JsonResult} from "../../../../models/jsonresult.model";

import {MessagesService} from '../../../../services/messages.service';
import {BaseObservableService} from "../../../../services/baseObservable.service";

declare var CompanySettings: any;

@Injectable()
export class AdminLoadFormService extends BaseObservableService {
    lineageFlag: string = '';
    aOptions: any[] = [];

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

    getActionOptions(): SelectItem[] {
        this.aOptions = [
            {label: 'Promotion', value: 'P'},
            {label: 'Relation', value: 'R'},
            {label: 'Responsibilities', value: 'O'},
            {label: 'Unrelation', value: 'U'},
            {label: 'Users/Groups', value: 'M'}
        ];

        if (CompanySettings != null && CompanySettings.UseLegacyLineage != null) {
            this.lineageFlag = CompanySettings.UseLegacyLineage;
        }

        if (this.lineageFlag == 'true') {
            this.aOptions.push({label: 'Lineage : Business', value: 'BL'});
            this.aOptions.push({label: 'Lineage : Technical', value: 'TL'});
        }

        return this.aOptions;
    }

    getTypeOptions(action: string): Observable<SelectItem[]> {
        return this.http.get(`/form/Load_TypeOptions?act=${action}`).pipe(
            map(responseTypeOptions => {
                let i = [];

                responseTypeOptions['forEach'](
                    typeOptionType => {
                        i.push({
                                label: typeOptionType.title,
                                value: typeOptionType.value
                            }
                        );
                    }
                );

                return <SelectItem[]>i;
            }),
            catchError(err => this.handleError(err))
        );
    }

    postLoad(model: LoadFilePostModel): Observable<JsonResult> {
        return this.http.post('form/AddLoad', model).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }
}
