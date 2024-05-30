import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { CreateRole, CreateSecurityPolicy, CreateSecurityPolicyOverride, ReadRole, ReadSecurityPolicy, ReadSecurityPolicyOverride } from '../models/security.model';

@Injectable({
    providedIn: 'root'
})
export class SecurityService extends BaseObservableService {
	baseUri: string = "api/v2/security";

	constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

	createPolicy(policy: CreateSecurityPolicy): Observable<ReadSecurityPolicy> {
		return this.http.post(`${this.baseUri}/policies`, policy)
			.pipe(
				map((response) => <ReadSecurityPolicy>response),
				catchError((err) => this.handleError(err))
			);
	}

	createPolicyOverride(override: CreateSecurityPolicyOverride): Observable<ReadSecurityPolicyOverride> {
		return this.http.post(`${this.baseUri}/policy-overrides`, override)
			.pipe(
				map((response) => <ReadSecurityPolicyOverride>response),
				catchError((err) => this.handleError(err))
			);
	}

    createRole(role: CreateRole): Observable<ReadRole> {
		return this.http.post(`${this.baseUri}/roles`, role)
            .pipe(
				map((response) => <ReadRole>response),
                catchError((err) => this.handleError(err))
            );
    }

	deletePolicy(uid: string): Observable<any> {
		const httpHeaders = {
			headers: new HttpHeaders({ 'Content-Type': 'application/json' })
		};
		return this.http.delete(`${this.baseUri}/policies/${uid}`, httpHeaders)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}

	deletePolicyOverride(uid: string): Observable<any> {
		const httpHeaders = {
			headers: new HttpHeaders({ 'Content-Type': 'application/json' })
		};
		return this.http.delete(`${this.baseUri}/policy-overrides/${uid}`, httpHeaders)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}

    deleteRole(uid: string): Observable<any> {
        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
		return this.http.delete(`${this.baseUri}/roles/${uid}`, httpHeaders)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }

	getPolicies(): Observable<ReadSecurityPolicy[]> {
		return this.http.get(`${this.baseUri}/policies`)
			.pipe(
				map((response) => <ReadSecurityPolicy[]>response),
				catchError((err) => this.handleError(err))
			);
	}

	getPolicyOverrides(): Observable<ReadSecurityPolicyOverride[]> {
		return this.http.get(`${this.baseUri}/policy-overrides`)
			.pipe(
				map((response) => <ReadSecurityPolicyOverride[]>response),
				catchError((err) => this.handleError(err))
			);
	}

    getRoles(): Observable<ReadRole[]> {
		return this.http.get(`${this.baseUri}/roles`)
            .pipe(
				map((response) => <ReadRole[]>response),
                catchError((err) => this.handleError(err))
            );
    }

	updatePolicy(policy: ReadSecurityPolicy): Observable<any> {
		return this.http.put(`${this.baseUri}/policies/${policy.uid}`, policy)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}

	updatePolicyOverride(override: ReadSecurityPolicyOverride): Observable<any> {
		return this.http.put(`${this.baseUri}/policy-overrides/${override.uid}`, override)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}

	updateRole(role: ReadRole): Observable<any> {
		return this.http.put(`${this.baseUri}/roles/${role.uid}`, role)
            .pipe(
                map((response) => response),
                catchError((err) => this.handleError(err))
            );
    }
}