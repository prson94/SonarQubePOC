///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { DetailField, DetailRow, DetailModel, IObjectDetailService } from '../models/object-detail.model';

@Injectable()
export class ObjectDetailService implements IObjectDetailService {

    constructor(private http: Http) { }

    getObjectDetail(objectID: number, objectType: string): Promise<DetailModel> {
        return this.http.get(`api/${objectType}/${objectID}/detail`)
            .toPromise()
            .then(response => <DetailModel>response.json())
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}