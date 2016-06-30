///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { IGroupService, GroupSearchResultModel, GroupResourceInfo, Group, GroupEditorModel, ResourceGroup } from '../models/group.model';

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

    putGroup(group: Group): Promise<any> {
        return this.http.put('form/Group', group)
            .toPromise()
            .catch(err => this.handleError(err));
    }
    
}

