///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { SocialComment, SocialVote } from '../models/social.model';

@Injectable()
export class SocialService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getComments(objectID: number, objectType: string, daysToLookBack: number, page?:number, count?:number): Promise<SocialComment[]> {        
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });

        this.addRequestVerificationHeaders(headers);

        return this.http
            .post(`services/community/comments`, `ObjectType=${objectType}&ObjectID=${objectID}&Skip=${page? page:0}&Take=${count? count: 10}&DateFilter=-${daysToLookBack}`,  { headers: headers })
            .toPromise()
            .then(res => <SocialComment[]>res.json())
            .catch(this.handleError);
    }

    vote(commentID: number, up: boolean): Promise<SocialVote>{
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });

        this.addRequestVerificationHeaders(headers);

        return this.http
            .post(`services/community/vote`, `CommentID=${commentID}&Vote=${up ? "1" : "0"}`, { headers: headers })
            .toPromise()
            .then(res => <SocialVote>res.json())
            .catch(this.handleError);
    }
}