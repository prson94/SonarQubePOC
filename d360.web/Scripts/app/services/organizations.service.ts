import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Organization, OrganizationDomain, OrganizationInvitation, OrganizationResource, OrganizationType, ContractModel, ContractType, Contract } from '../models/organization.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class OrganizationsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getOrganizationTypes(): Promise<OrganizationType[]> {
        return this.http.get('services/organizations/types')
            .toPromise()
            .then(response => <OrganizationType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getOrganizations(id: number): Promise<Organization[]> {
        return this.http.get(`services/organizations/${id}/items`)
            .toPromise()
            .then(response => <Organization[]>response.json())
            .catch(err => this.handleError(err));
    }

    getOrganizationsByType(id: number): Promise<Organization[]> {
        return this.http.get(`services/organizations/${id}/items`)
            .toPromise()
            .then(response => <Organization[]>response.json())
            .catch(err => this.handleError(err));
    }

    getDefaultContracts(): Promise<ContractModel[]> {
        return this.http.get(`services/organizations/default/contracts`)
            .toPromise()
            .then(response => <ContractModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    getContractsByOrganization(id: number): Promise<ContractModel[]> {        
        return this.http.get(`services/organizations/${id}/contracts`)
            .toPromise()
            .then(response => <ContractModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    getDomainsByOrganization(id: number): Promise<OrganizationDomain[]> {
        return this.http.get(`services/organizations/${id}/domains`)
            .toPromise()
            .then(response => <OrganizationDomain[]>response.json())
            .catch(err => this.handleError(err));
    }

    getInvitationsByOrganization(id: number): Promise<OrganizationInvitation[]> {
        return this.http.get(`services/organizations/${id}/invitations`)
            .toPromise()
            .then(response => <OrganizationInvitation[]>response.json())
            .catch(err => this.handleError(err));
    }

    getUsersByOrganization(id: number): Promise<OrganizationResource[]> {
        return this.http.get(`services/organizations/${id}/users`)
            .toPromise()
            .then(response => <OrganizationResource[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteOrganizationType(id: number): Promise<JsonResult> {
        return this.http.delete(`/form/AssetType?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    saveOrganization(organization: Organization): Promise<JsonResult> {
        if (organization.ID == undefined || !organization.ID) {
                return this.postDynamic(this.http, 'organization', organization);
            }
        return this.putDynamic(this.http, 'organization', organization);
    }

    deleteOrganization(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'organization', id);
    }

    getContract(id: number): Promise<Contract> {
        return this.http.get(`form/Contract/${id}`)
            .toPromise()
            .then(res => <Contract>res.json())
            .catch(err => this.handleError(err));
    }

    putContract(contract: Contract, publish: boolean = false): Promise<JsonResult> {
        return this.http.put(`form/Contract?publish=${publish}`, contract)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));

    }

    postContract(contract: Contract, publish: boolean = false): Promise<JsonResult> {
        return this.http.post(`form/Contract?publish=${publish}`, contract)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));

    }

    deleteContract(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'contract', id);
    }

    saveDomain(domain: OrganizationDomain): Promise<JsonResult> {
        if (domain.ID == undefined || !domain.ID) {
            return this.postDynamic(this.http, 'organizationdomain', domain);
        }
        return this.putDynamic(this.http, 'organizationdomain', domain);
    }

    deleteDomain(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'organizationdomain', id);
    }

    saveInvitation(invitation: OrganizationInvitation): Promise<JsonResult> {
        if (invitation.ID == undefined || !invitation.ID) {
            return this.postDynamic(this.http, 'organizationinvitation', invitation);
        }
        return this.putDynamic(this.http, 'organizationinvitation', invitation);
    }

    deleteInvitation(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'organizationinvitation', id);
    }
}