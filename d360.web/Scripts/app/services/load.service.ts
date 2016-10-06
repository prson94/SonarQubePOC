
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { LoadDetail, LoadFilePostModel } from '../models/load.model';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { GridColumn } from '../models/grid-definition.model';
import { SelectItem } from 'primeng/primeng';
import { JsonResult } from '../models/form.model';


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
            { label: 'Please Choose...', value: '' },
            { label: 'Promotion', value: 'P' },
            { label: 'Relation', value: 'R' },
            { label: 'Unrelation', value: 'U' },
            { label: 'Lineage', value: 'L' },
            { label: 'Synonyms', value: 'S' }
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

    getExpectedColumns(type: string, id: number): Promise<string[]>  {
        return this.http.get(`form/Load_ExpectedColumns?id=${id}&type=${type}`)
            .toPromise()
            .then(response => <string[]>response.json())
            .catch(err => this.handleError(err));
    }

    getExpectedColumnsExcel(type: string, id: number): Promise<any> {
        return this.http.get(`form/Load_ExpectedColumns_ToExcel?id=${id}&type=${type}`)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    postLoad(model: LoadFilePostModel): Promise<JsonResult> {
        return this.http.post('form/AddLoad', model)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }
}