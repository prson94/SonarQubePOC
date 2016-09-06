///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { FollowDetail } from '../models/follower.model';


@Injectable()
export class FollowerService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFollowers(type: string, id: number): Promise<FollowDetail[]> {
        return this.http.get(`api/${type}/${id}/followers`)
            .toPromise()
            .then(response => <FollowDetail[]>response.json())
            .catch(err => this.handleError(err));

    }
}