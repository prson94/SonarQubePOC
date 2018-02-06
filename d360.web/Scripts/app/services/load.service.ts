import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { LoadDetail, LoadFilePostModel, LoadColumn, LoadColumnValue } from '../models/load.model';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { GridColumn } from '../models/grid-definition.model';
import { SelectItem } from 'primeng/components/common/api';
import { JsonResult } from '../models/jsonresult.model';


@Injectable()
export class LoadService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getLoads(): Promise<LoadDetail[]> {
        return this.http.get('api/loads')
            .toPromise()
            .then(response => <LoadDetail[]>response.json())
            .catch(err => this.handleError(err));
    }

    getLoadColumns(id: number): Promise<GridColumn[]> {
        return this.http.get(`api/loads/${id}/columns`)
            .toPromise()
            .then(response => <GridColumn[]>response.json())
            .catch(err => this.handleError(err));
    }

    getLoadItems(id: number): Promise<any[]> {
        return this.http.get(`api/loads/${id}/items`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getActionOptions(): SelectItem[] {
        return [
            { label: 'Promotion', value: 'P' },
            { label: 'Relation', value: 'R' },
            { label: 'Responsibilities', value: 'O' },
            { label: 'Unrelation', value: 'U' }//,
            { label: 'Lineage : Business', value: 'BL' },
            { label: 'Lineage : Technical', value: 'TL' }//,
            //{ label: 'Synonyms', value: 'S' }
        ];
    }


    getTypeOptions(action: string): Promise<SelectItem[]> {
        return this.http.get(`/form/Load_TypeOptions?act=${action}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .then(response => {
                let i = [];
                response.forEach(r => {
                    i.push({ label: r.title, value: r.value });
                });
                return <SelectItem[]>i;
            })
            .catch(err => this.handleError(err));
    }

    getExpectedColumns(action: string, type: string, id: number): Promise<LoadColumn[]>  {
        return this.http.get(`form/Load_ExpectedColumns?action=${action}&id=${id}&type=${type}`)
            .toPromise()
            .then(response => <LoadColumn[]>response.json())
            .catch(err => this.handleError(err));
    }

    getExpectedColumnsExcel(action: string, type: string, id: number): Promise<LoadColumn[]> {
        return this.http.get(`form/Load_ExpectedColumns_ToExcel?action=${action}&id=${id}&type=${type}`)
            .toPromise()
            .then(response => <LoadColumn[]>response.json())
            .catch(err => this.handleError(err));
    }

    getLoadErrorsXls(id: number) {
        window.location.assign(`/form/loads/${id}/Errors.xlsx`);
    }

    getLoadOriginalXls(id: number) {
        window.location.assign(`/form/loads/${id}/all.xlsx`);
    }

    postLoad(model: LoadFilePostModel): Promise<JsonResult> {
        return this.http.post('form/AddLoad', model)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }
}