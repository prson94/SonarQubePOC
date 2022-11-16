import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { PolicyType } from '../models/policy.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';

@Injectable({
    providedIn: 'root'
})
export class PoliciesService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

	getPolicyType(assetTypeUid: string): Observable<PolicyType> {
		return this.http.get(`api/policytypes/${assetTypeUid}`)
            .pipe(
                map((response) => <PolicyType>response),
                catchError((err) => this.handleError(err))
            );
    }
}
