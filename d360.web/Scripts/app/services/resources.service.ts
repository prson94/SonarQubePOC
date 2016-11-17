import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Resource, CountObject, ResponsibilityDetailForResource, FollowingDetailForResource, ResourceAPICredentials } from '../models/resource.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class ResourcesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResources(): Promise<Resource[]> {
        return this.http.get('/api/resources/1')
            .toPromise()
            .then(response => <Resource[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResource(id: number): Promise<Resource> {
        return this.http.get(`/api/resources/1/${id}`)
            .toPromise()
            .then(response => <Resource>response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilityBreakdownByResource(id: number): Promise<CountObject[]> {
        return this.http.get(`tiles/ResponsibilityBreakdownByResource?id=${id}`)
            .toPromise()
            .then(response => <CountObject[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFollowingBreakdownByResource(id: number): Promise<CountObject[]> {
        return this.http.get(`tiles/FollowingBreakdownByResource?id=${id}`)
            .toPromise()
            .then(response => <CountObject[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilitiesByResourceByType(type: string, id: number, targetType: string, targetId: number): Promise<ResponsibilityDetailForResource[]> {
        return this.http.get(`api/${type}/${id}/ownership/${targetType}/${targetId}`)
            .toPromise()
            .then(response => <ResponsibilityDetailForResource[]>response.json())
            .catch(err => this.handleError(err));
    }

    //    public JsonNetResult GetFollowingByResourceByType(int resourceID, string type, int id)
    getFollowingByResourceByType(resourceID: number, type: string, id: number): Promise<FollowingDetailForResource[]> {
        return this.http.get(`queries/followingbyresourcebytype?resourceID=${resourceID}&type=${type}&id=${id}`)
            .toPromise()
            .then(response => <FollowingDetailForResource[]>response.json())
            .catch(err => this.handleError(err));
    }

    exportFollowingByResourceByType(resourceID: number, type: string, id: number) {
        window.location.assign(`/resources/${resourceID}/following/${type}/${id}.xlsx`);      
    }

    exportResponsibilitiesByResourceByType(resourceID: number, type: string, id: number) {
        window.location.assign(`/resources/${resourceID}/ownership/${type}/${id}.xlsx`);   
    }

    getMyCredentials(): Promise<ResourceAPICredentials> {
        return this.http.get('overlays/myapicredentialsng')
            .toPromise()
            .then(response => <ResourceAPICredentials>response.json())
            .catch(err => this.handleError(err));
    }

    getUserGroups(resourceID: number): Promise<any[]> {
        return this.http.get(`resources/_GroupsByResourceID?id=${resourceID}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAuthenticationModel(): Promise<any> {        
        return this.http.get('api/authenticationModel')
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    resetResourcesPassword(resourceID: number): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' //pass as text since its a dynamic object and mvc has issue with dynamic models
        });

        return this.http
            .post(`form/ResetResourcePassword`, 'ID=' + resourceID, { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }
}