///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { GovernanceItem, IGovernanceService } from '../models/governance.model';
import { MessagesService } from './index';
import { BaseService } from './base.service';

@Injectable()
export class GovernanceService extends BaseService implements IGovernanceService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getGovernanceItems(): Promise<GovernanceItem[]> {
        return this.http.get('api/ownership/types')
            .toPromise()
            .then(response => <GovernanceItem[]>response.json())
            .catch(err=>this.handleError(err));
    }
}