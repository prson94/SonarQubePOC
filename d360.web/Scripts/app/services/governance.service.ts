///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { GovernanceItem, IGovernanceService } from '../models/governance.model';

@Injectable()
export class GovernanceService implements IGovernanceService {

    constructor(private http: Http) { }

    getGovernanceItems(): Promise<GovernanceItem[]> {
        return this.http.get('api/ownership/types')
            .toPromise()
            .then(response => <GovernanceItem[]>response.json())
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}