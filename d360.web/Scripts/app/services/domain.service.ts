///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { DomainType, IDomainService } from '../models/domain.model';

@Injectable()
export class DomainService implements IDomainService {

    constructor(private http: Http) { }

    getDomains(): Promise<DomainType[]> {
        return this.http.get('services/domains')
            .toPromise()
            .then(response => <DomainType[]>response.json())
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}