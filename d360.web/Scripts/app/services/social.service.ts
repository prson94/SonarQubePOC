import { Injectable } from '@angular/core';
import { SocialComment, SocialVote, SocialVoteType, SocialEditCommentData } from '../models/social.model';
import { Count } from '../models/counts.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class SocialService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getComments(objectID: number, objectType: string, daysToLookBack: number, page?: number, count?: number, typeFilter?: number): Observable<SocialComment[]> {        
        let headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });
        
        return this.http
            .post(`api/v2/social/comments`, `IsNg=true&ObjectType=${objectType}&ObjectID=${objectID > 0 ? objectID : ''}&Skip=${page ? page : 0}&Take=${count ? count : 10}&DateFilter=-${daysToLookBack}&TypeFilter=${typeFilter == undefined ? '' : typeFilter}`,  { headers })
            .pipe(
            map(res => <SocialComment[]>res),
            catchError(err => this.handleError(err))
            );
    }

    vote(commentID: number, vote: SocialVoteType): Observable<SocialVote[]>{
        let headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });
        
        return this.http
            .post('api/v2/social/vote', `CommentID=${commentID}&Vote=${vote}`, { headers })
            .pipe(
            map(res => <SocialVote[]>res),
            catchError(err => this.handleError(err))
            );
    }

    editComment(commentEditData: SocialEditCommentData): Observable<SocialComment> {
        let headers = new HttpHeaders();

        headers.append('Content-Type', 'application/json');
        
        return this.http
            .post('api/v2/social/edit', commentEditData, { headers })
            .pipe(
            map(res => <SocialComment>res),
            catchError(err => this.handleError(err))
            );
    }

    addComment(commentAddData: SocialEditCommentData): Observable<SocialComment> {
        let headers = new HttpHeaders();

        headers.append('Content-Type', 'application/json');
        
        return this.http
            .post('api/v2/social/comment', commentAddData, { headers })
            .pipe(
            map(res => <SocialComment>res),
            catchError(err => this.handleError(err))
            );
    }

    getMyCounts(daysToLookBack: number): Observable<Count[]> {
        let resourceID = -1;
        return this.http.get(`api/v2/social/count/${resourceID}/${daysToLookBack}`)
            .pipe(
            map(response => <Count[]>response),
            catchError(err => this.handleError(err))
            );
    }

    getTheCounts(resourceID: number, daysToLookBack: number): Observable<Count[]> {
        return this.http.get(`api/v2/social/count/${resourceID}/${daysToLookBack}`)
            .pipe(
            map(response => <Count[]>response),
            catchError(err => this.handleError(err))
            );
    }

}