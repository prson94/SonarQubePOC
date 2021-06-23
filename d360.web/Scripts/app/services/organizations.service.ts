import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {catchError, map} from 'rxjs/operators';

import {
    Organization,
    OrganizationDomain,
    OrganizationInvitation,
    OrganizationResource,
    OrganizationType,
    Contract,
    ContractDetail,
    ContractAcceptanceDetail
} from '../models/organization.model';
import {JsonResult} from '../models/jsonresult.model';

import { MessagesObservableService } from './messages-observable.service';
import {BaseObservableService} from './baseObservable.service';

@Injectable({
    providedIn: 'root'
})
export class OrganizationsService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getOrganizationTypes(): Observable<OrganizationType[]> {
        return this.http.get('services/organizations/types')
            .pipe(
                map(response => <OrganizationType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getOrganizations(id: number): Observable<Organization[]> {
        return this.http.get(`services/organizations/${id}/items`)
            .pipe(
                map(response => <Organization[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getOrganizationsByType(id: number): Observable<Organization[]> {
        return this.http.get(`services/organizations/${id}/items`)
            .pipe(
                map(response => <Organization[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getDefaultContracts(): Observable<ContractDetail[]> {
        return this.http.get(`services/organizations/default/contracts`)
            .pipe(
                map(response => <ContractDetail[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getContractsByOrganization(id: number): Observable<ContractDetail[]> {
        return this.http.get(`services/organizations/${id}/contracts`)
            .pipe(
                map(response => <ContractDetail[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getDomainsByOrganization(id: number): Observable<OrganizationDomain[]> {
        return this.http.get(`services/organizations/${id}/domains`)
            .pipe(
                map(response => <OrganizationDomain[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getInvitationsByOrganization(id: number): Observable<OrganizationInvitation[]> {
        return this.http.get(`services/organizations/${id}/invitations`)
            .pipe(
                map(response => <OrganizationInvitation[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getUsersByOrganization(id: number): Observable<OrganizationResource[]> {
        return this.http.get(`services/organizations/${id}/users`)
            .pipe(
                map(response => <OrganizationResource[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteOrganizationType(id: number): Observable<JsonResult> {
        return this.http.delete(`/form/OrganizationType?id=${id}`)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    saveOrganization(organization: Organization): Observable<JsonResult> {
        if (organization.ID == undefined || !organization.ID) {
            return this.postDynamic(this.http, 'organization', organization);
        }
        return this.putDynamic(this.http, 'organization', organization);
    }

    deleteOrganization(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'organization', id);
    }

    getContract(id: number): Observable<Contract> {
        return this.http.get(`form/Contract/${id}`)
            .pipe(
                map(res => <Contract>res),
                catchError(err => this.handleError(err))
            );
    }

    putContract(contract: Contract, publish: boolean = false): Observable<JsonResult> {
        return this.http.put(`form/Contract?publish=${publish}`, contract)
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );

    }

    postContract(contract: Contract, publish: boolean = false): Observable<JsonResult> {
        return this.http.post(`form/Contract?publish=${publish}`, contract)
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );

    }

    deleteContract(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'contract', id);
    }

    saveDomain(domain: OrganizationDomain): Observable<JsonResult> {
        if (domain.ID == undefined || !domain.ID) {
            return this.postDynamic(this.http, 'organizationdomain', domain);
        }
        return this.putDynamic(this.http, 'organizationdomain', domain);
    }

    deleteDomain(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'organizationdomain', id);
    }

    saveInvitation(invitation: OrganizationInvitation): Observable<JsonResult> {
        if (invitation.ID == undefined || !invitation.ID) {
            return this.postDynamic(this.http, 'organizationinvitation', invitation);
        }
        return this.putDynamic(this.http, 'organizationinvitation', invitation);
    }

    deleteInvitation(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'organizationinvitation', id);
    }

    getContractHistoryForResource(id: number): Observable<ContractAcceptanceDetail[]> {
        return this.http.get(`services/organizations/history/resource/${id}`)
            .pipe(
                map(res => <ContractAcceptanceDetail[]>res),
                catchError(err => this.handleError(err))
            );
    }

    getContractHistoryForContract(id: number): Observable<ContractAcceptanceDetail[]> {
        return this.http.get(`services/organizations/history/contract/${id}`)
            .pipe(
                map(res => <ContractAcceptanceDetail[]>res),
                catchError(err => this.handleError(err))
            );
    }

    getContractHistoryForOrganization(id: number): Observable<ContractAcceptanceDetail[]> {
        return this.http.get(`services/organizations/history/organization/${id}`)
            .pipe(
                map(res => <ContractAcceptanceDetail[]>res),
                catchError(err => this.handleError(err))
            );
    }
}
