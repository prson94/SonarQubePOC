import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { IGroupService, GroupSearchResultModel, GroupResourceInfo, Group, GroupEditorModel, ResourceGroup } from '../models/group.model';
import { JsonResult } from '../models/jsonresult.model';
import { CountObject } from '../models/resource.model';

@Injectable()
export class GroupService extends BaseService implements IGroupService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getGroupList(): Promise<GroupSearchResultModel[]> {
        return this.http.get('api/groups')
            .toPromise()
            .then(r => <GroupSearchResultModel[]>r.json())
            .catch(err => this.handleError(err));
    }

    getGroupResourceList(id: number): Promise<GroupResourceInfo[]> {
        return this.http.get(`api/groups/${id}/resources`)
            .toPromise()
            .then(response => <GroupResourceInfo[]>response.json())
            .catch(err => this.handleError(err));
    }

    getGroup(id: number): Promise<GroupEditorModel> {
        return this.http.get(`form/Group?id=${id}`)
            .toPromise()
            .then(response => <GroupEditorModel>response.json())
            .catch(err => this.handleError(err)); 
    }

    putGroup(group: Group): Promise<JsonResult> {
        return this.http.put('form/Group', group)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    postGroup(group: Group): Promise<JsonResult> {
        return this.http.post('form/Group', group)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    deleteGroup(id: number): Promise<JsonResult> {
        return this.http.delete(`form/DeleteGroupByID?id=${id}`)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    postResourceGroup(resourceGroups: ResourceGroup[]): Promise<JsonResult> {
        return this.http.post('form/ResourceGroup', resourceGroups)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    deleteResourceGroup(groupID: number, resourceID: number): Promise<JsonResult> {
        return this.http.delete(`form/ResourceGroup?groupID=${groupID}&resourceID=${resourceID}`)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    getGroupUserList(id: number, pagenum: number, pagesize: number, sortDataField: string, sortOrder: string): Promise<any> {
        return this.http.get(`form/GetGroupUserList?id=${id}&pagenum=${pagenum}&pagesize=${pagesize}&sortdatafield=${sortDataField}&sortorder=${sortOrder}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
    
    getResponsibilityBreakdownByGroup(id: number): Promise<CountObject[]> {
        return this.http.get(`/api/v2/social//ResponsibilityBreakdownByGroup?id=${id}`)
            .toPromise()
            .then(response => <CountObject[]>response.json())
            .catch(err => this.handleError(err));
    }

}

