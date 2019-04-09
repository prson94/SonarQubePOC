import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { SocialComment, SocialVote, SocialVoteType, SocialEditCommentData } from '../models/social.model';
import { Count } from '../models/counts.model';

@Injectable()
export class SocialService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getComments(objectID: number, objectType: string, daysToLookBack: number, page?:number, count?:number, typeFilter?:number): Promise<SocialComment[]> {        
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });
        
        return this.http
            .post(`api/v2/social/comments`, `IsNg=true&ObjectType=${objectType}&ObjectID=${objectID > 0 ? objectID : ''}&Skip=${page ? page : 0}&Take=${count ? count : 10}&DateFilter=-${daysToLookBack}&TypeFilter=${typeFilter == undefined ? '' : typeFilter}`,  { headers: headers })
            .toPromise()
            .then(res => <SocialComment[]>res.json())
            .catch(err => this.handleError(err));
    }

    vote(commentID: number, vote: SocialVoteType): Promise<SocialVote[]>{
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });
        
        return this.http
            .post('api/v2/social/vote', `CommentID=${commentID}&Vote=${vote}`, { headers: headers })
            .toPromise()
            .then(res => <SocialVote[]>res.json())
            .catch(err => this.handleError(err));
    }

    editComment(commentEditData: SocialEditCommentData): Promise<SocialComment> {
        let headers = new Headers();

        headers.append('Content-Type', 'application/json');
        
        return this.http
            .post('api/v2/social/edit', commentEditData, { headers: headers })
            .toPromise()
            .then(res => <SocialComment>res.json())
            .catch(err => this.handleError(err));
    }

    addComment(commentAddData: SocialEditCommentData): Promise<SocialComment> {
        let headers = new Headers();

        headers.append('Content-Type', 'application/json');
        
        return this.http
            .post('api/v2/social/comment', commentAddData, { headers: headers })
            .toPromise()
            .then(res => <SocialComment>res.json())
            .catch(err => this.handleError(err));
    }

    getMyCounts(daysToLookBack: number): Promise<Count[]> {
        return this.http.get(`api/v2/count/social/${daysToLookBack}`)
            .toPromise()
            .then(response => <Count[]>response.json())
            .catch(err => this.handleError(err));
    }

    getTheCounts(resourceID: number, daysToLookBack: number): Promise<Count[]> {
        return this.http.get(`api/v2/counts/${resourceID}/${daysToLookBack}`)
            .toPromise()
            .then(response => <Count[]>response.json())
            .catch(err => this.handleError(err));
    }

}