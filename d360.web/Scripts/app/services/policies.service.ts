import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {catchError, map} from 'rxjs/operators';

import {PolicyType, Policy} from '../models/policy.model';
import {JsonResult} from '../models/jsonresult.model';

import {MessagesService} from './messages.service';
import {BaseObservableService} from './baseObservable.service';

@Injectable()
export class PoliciesService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getPolicyTypes(): Observable<PolicyType[]> {
        return this.http.get('api/policytypes')
            .pipe(
                map(response => <PolicyType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getPolicies(
        policyTypeId: number,
        stripHtml: boolean = false
    ): Observable<Policy[]> {
        return this.http.get(`api/policytypes/${policyTypeId}/policies?stripHtml=${stripHtml}`)
            .pipe(
                map(response => <Policy[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getPolicyType(id: number): Observable<PolicyType> {
        return this.http.get(`api/policytypes/${id}`)
            .pipe(
                map(response => <PolicyType>response),
                catchError(err => this.handleError(err))
            );
    }

    deletePolicy(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'policy', id);
    }

    savePolicy(policy: Policy): Observable<JsonResult> {
        let methodName = 'putDynamic';

        if (policy.ID == undefined || !policy.ID) {
            methodName = 'postDynamic';
        }

        return this[methodName](this.http, 'policy', policy);
    }
}
