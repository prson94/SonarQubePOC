import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { FollowDetail, FollowInfo } from '../models/follower.model';


@Injectable()
export class FollowerService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFollowers(type: string, id: number): Promise<FollowDetail[]> {
        return this.http.get(`api/${type}/${id}/followers`)
            .toPromise()
            .then(response => <FollowDetail[]>response.json())
            .catch(err => this.handleError(err));

    }

    getFollowInfo(type: string, id: number): Promise<FollowInfo> {
        return this.http.get(`api/followinfo/${type}/${id}`)
            .toPromise()
            .then(response => <FollowInfo>response.json())
            .catch(err => this.handleError(err));
    }

    updateFollowStatus(type: string, id: number, includeChildren: boolean = false): Promise<any> {
        return this.http.post('resources/UpdateFollowStatus', { type: type, id: id, includeChildren: includeChildren })
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

}