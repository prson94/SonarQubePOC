import { catchError, map } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Tag, TagType } from '../models/tag.model';
import { Observable } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class TagService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getTags(phrase: string, excludeObjects: string = ''): Observable<Tag[]> {
        let url = `api/tagsuggestions?phrase=${phrase}&excludeObjects=${excludeObjects}`;

        return this.http.get(url).pipe(
            map(response => {
                return response
            }),
            map(item => { return <Tag[]>item }),
            catchError(err => this.handleError(err)));
    }

    getTagsList(): Observable<TagType[]> {
        let url = `api/v2/tags`;
        return this.http.get(url)
            .pipe(map(response => <any>response),
                map(items => <TagType[]>items.items),
                catchError(err => this.handleError(err)));

    }

    deleteTagByUid(uid: string): Observable<any> {
        let url = `api/v2/tags/${uid}`;
        return this.http.delete(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    deleteTags(tags: TagType[]): Observable<any> {
        let url = `api/v2/tags/`;

        if (tags.length == 1)
            return this.deleteTagByUid(tags[0].uid);

        let body: any[] = []
        tags.forEach(t => {
            body.push({ 'uid': t.uid });
        })

        const httpHeaders = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }), body: body
        };
        return this.http.delete(url, httpHeaders)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    saveTag(tag: TagType): Observable<any> {
        let url = `api/v2/tags/`;

        if (tag.uid == undefined || !tag.uid) {
            return this.http.post(url, tag)
                .pipe(map(response => <any>response),
                    catchError(err => this.handleError(err, true)));
        }
        url = `api/v2/tags/${tag.uid}`;
        return this.http.put(url, tag)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }
    consolidateTags(parentTag: string, childrenTags: string[]): Observable<any[]> {
        let url = `api/v2/tags/consolidate/${parentTag}`;
        return this.http.post(url, childrenTags)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    getAssetPathsForTag(tagUid: string): Observable<any[]> {
        let url = `api/v2/tags/${tagUid}/assetpath`;
        return this.http.get(url)
            .pipe(map(response => <any[]>response),
                catchError(err => this.handleError(err, true)))
    }

    searchTags(q: string, exceptId): Observable<any[]> {
        let url = `api/v2/tags/search/${q}`;
        return this.http.get(url)
            .pipe(map(response => <any[]>response),
                catchError(err => this.handleError(err, true)))
    }

    setTaggingStatus(state: boolean): Observable<any> {
        let url = `api/v2/tags/settaggingstatus`;
        let body = { IsTaggingEnabled: state };
        return this.http.put(url, body)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)))
    }

    exportTags() {
        this.http.get('api/v2/tags/export/', { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'tags'));
    }

    downloadFile(data: Blob, name: string) {
        var filename = `${name} ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }


}