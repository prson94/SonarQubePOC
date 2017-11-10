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

    getPolicies(policyTypeId: number, stripHtml: boolean = false): Promise<Policy[]> {
        return this.http.get(`api/policytypes/${policyTypeId}/policies?stripHtml=${stripHtml}`)
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

    deletePolicy(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'policytype', id);
    }
    
    savePolicyType(policyType: PolicyType): Promise<JsonResult> {
        if (policyType.ID == undefined || !policyType.ID) {
            return this.postDynamic(this.http, 'policytype', policyType);
        }
        return this.putDynamic(this.http, 'policytype', policyType);
    }

    deletePolicyItem(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'policy', id);
    }

    savePolicy(policy: Policy): Promise<JsonResult> {
        if (policy.ID == undefined || !policy.ID) {
            return this.postDynamic(this.http, 'policy', policy);
        }
        return this.putDynamic(this.http, 'policy', policy);
    }    
}