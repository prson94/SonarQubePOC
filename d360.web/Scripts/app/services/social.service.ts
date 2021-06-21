import { Injectable } from "@angular/core";
import { CommentApiPutModel, Emoji, CommentApiPostModel, CommentDetail, CommentDetails, CommentVoteDetail } from "../models/social.model";
import { Count } from "../models/counts.model";
import { HttpClient, HttpErrorResponse, HttpHeaders } from "@angular/common/http";
import { catchError, map } from "rxjs/operators";
import { Observable } from "rxjs";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";
import { Router } from "@angular/router";

@Injectable({
    providedIn: 'root'
})
export class SocialService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService, private router: Router) { super(messagesService); }

    getComments(assetUid: string, daysToLookBack: number, page?: number, count?: number, typeFilter?: number): Observable<CommentDetails> {        
        return this.http
            .get(`api/v2/comments?assetUid=${assetUid}&_pageNum=${page ? page : 0}&_pageSize=${count ? count : 10}`)
            .pipe(
                map((res) => <CommentDetails>res),
                catchError((err) => err instanceof HttpErrorResponse && err.status === 404 ? this.handleError(err, false, this.router) : this.handleError(err))                
            );
    }

    getCommentForFollowers(followerUid: string, daysToLookBack: number, page?: number, count?: number, typeFilter?: number): Observable<CommentDetails> {
        var qString = `&followerUidIsCurrentResourceUID=true&daysToLookBack=${daysToLookBack}&ShowDeleteComment=false`;
        if (typeFilter != null && typeFilter > 0) {
            qString = qString + `&CommentTypeID=${typeFilter}`;
        }
        return this.http
            .get(`api/v2/comments?followerUid=${followerUid}&_pageNum=${page ? page : 0}&_pageSize=${count ? count : 10}${qString}`)
            .pipe(
                map((res) => <CommentDetails>res),
                catchError((err) => this.handleError(err))
            );
    }

    getCommentVotes(commentUid: string): Observable<CommentVoteDetail[]> {
        return this.http
            .get(`api/v2/comments/${commentUid}/votes`)
            .pipe(
                map((res) => <CommentVoteDetail[]>res),
                catchError((err) => this.handleError(err))
            );
    }

    addVote(commentUid: string, emoji: Emoji): Observable<boolean>{
       
        return this.http
            .post(`api/v2/comments/${commentUid}/votes/${emoji}`, {}, { observe: "response" })
            .pipe(
                map((res) => (res.status == 200 || res.status == 201)),
                catchError((err) => this.handleError(err))
            );
    }

    deleteVote(commentUid: string, emoji: Emoji): Observable<boolean> {

        return this.http
            .delete(`api/v2/comments/${commentUid}/votes/${emoji}`, { observe: "response" })
            .pipe(
                map((res) => (res.status == 200)),
                catchError((err) => this.handleError(err))
            );
    }

    addComment(comment: CommentApiPostModel): Observable<CommentDetail> {
        let headers = new HttpHeaders();

        headers.append("Content-Type", "application/json");
        return this.http
            .post("api/v2/comments", comment, { headers })
            .pipe(
                map((res) => <CommentDetail>res),
                catchError((err) => this.handleError(err))
            );
    }

    editComment(comment: CommentApiPutModel): Observable<boolean> {
        const httpOptions = {
            headers: new HttpHeaders({ "Content-Type": "application/json" }),
            observe: "response"
        };
        return this.http
            .put(`api/v2/comments/${comment.Uid}`, comment, {observe: "response"})//httpOptions)
            .pipe(
                map((res) => (res.status == 200)),
                catchError((err) => this.handleError(err))
            );
    }

    deleteComment(commentUid: string): Observable<boolean> {
        return this.http
            .delete(`api/v2/comments/${commentUid}`, { observe: "response" })
            .pipe(
                map((res) => (res.status == 200)),
                catchError((err) => this.handleError(err))
            );
    }

    getMyCounts(daysToLookBack: number): Observable<Count[]> {
        return this.http.get(`api/v2/comments/count/0/${daysToLookBack}`)
            .pipe(
            map((response) => <Count[]>response),
            catchError((err) => this.handleError(err))
            );
    }

    getTheCounts(resourceID: number, daysToLookBack: number): Observable<Count[]> {
        return this.http.get(`api/v2/comments/count/${resourceID}/${daysToLookBack}`)
            .pipe(
            map((response) => <Count[]>response),
            catchError((err) => this.handleError(err))
            );
    }

}