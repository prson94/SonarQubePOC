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

    getComments(assetUid: string, daysToLookBack: number, page?: number, count?: number, typeFilter?: number): Observable<SocialComment[]> {        
        //let headers = new HttpHeaders({
        //    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        //});
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };

        return this.http
            .post(`api/v2/comments`, `?AssetUid=${assetUid}&_pageNum=${page ? page : 0}&_pageSize=${count ? count : 10}`, httpOptions)
            .pipe(
                map(res => <SocialComment[]>res),
                catchError(err => this.handleError(err))
            );
    }

    addVote(commentUid: string, emoji: string): Observable<SocialVote[]>{
       
        return this.http
            .post(`api/v2/comments/${commentUid}/votes/${emoji}`, {})
            .pipe(
                map(res => <SocialVote[]>res),
                catchError(err => this.handleError(err))
            );
    }

    deleteVote(commentUid: string, emoji: string): Observable<SocialVote[]> {

        return this.http
            .delete(`api/v2/comments/${commentUid}/votes/${emoji}`)
            .pipe(
                map(res => <SocialVote[]>res),
                catchError(err => this.handleError(err))
            );
    }

    addComment(commentAddData: SocialEditCommentData): Observable<SocialComment> {
        let headers = new HttpHeaders();

        headers.append('Content-Type', 'application/json');
        return this.http
            .post('api/v2/comments', commentAddData, { headers })
            .pipe(
                map(res => <SocialComment>res),
                catchError(err => this.handleError(err))
            );
    }

    editComment(commentEditData: SocialEditCommentData): Observable<SocialComment> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        return this.http
            .put(`api/v2/comments/${commentEditData.Comment.Uid}`, commentEditData, httpOptions)
            .pipe(
                map(res => <SocialComment>res),
                catchError(err => this.handleError(err))
            );
    }

    deleteComment(commentUid: string): Observable<SocialComment> {
        let headers = new HttpHeaders();

        headers.append('Content-Type', 'application/json');
        return this.http
            .delete(`api/v2/comments/${commentUid}`, { headers })
            .pipe(
                map(res => <SocialComment>res),
                catchError(err => this.handleError(err))
            );
    }

    getMyCounts(daysToLookBack: number): Observable<Count[]> {
        return this.http.get(`api/v2/comments/count/0/${daysToLookBack}`)
            .pipe(
            map(response => <Count[]>response),
            catchError(err => this.handleError(err))
            );
    }

    getTheCounts(resourceID: number, daysToLookBack: number): Observable<Count[]> {
        return this.http.get(`api/v2/comments/count/${resourceID}/${daysToLookBack}`)
            .pipe(
            map(response => <Count[]>response),
            catchError(err => this.handleError(err))
            );
    }

}