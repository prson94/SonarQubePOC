
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { PolicyType, Policy } from '../models/policy.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class PoliciesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getPolicyTypes(): Promise<PolicyType[]> {
        return this.http.get('api/policytypes')
            .toPromise()
            .then(response => <PolicyType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getPolicies(policyTypeId: number): Promise<Policy[]> {
        return this.http.get(`api/policytypes/${policyTypeId}/policies`)
            .toPromise()
            .then(response => <Policy[]>response.json())
            .catch(err => this.handleError(err));
    }

    getPolicyType(id: number): Promise<PolicyType> {
        return this.http.get(`api/policytypes/${id}`)
            .toPromise()
            .then(response => <PolicyType>response.json())
            .catch(err => this.handleError(err));
    }

    deletePolicy(id: number) {
        return this.deleteDynamic(this.http, 'policytype', id);
    }
    
    saveDimension(policyType: PolicyType): Promise<JsonResult> {
        if (policyType.ID == undefined || !policyType.ID) {
            return this.postDynamic(this.http, 'policytype', policyType);
        }
        return this.putDynamic(this.http, 'policytype', policyType);
    }
}