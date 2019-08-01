import { catchError, map } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Tag, TagType } from '../models/tag.model';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

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

    getTagById(id: number): Observable<TagType> {
        let url = `api/v2/tags?id=${id}`;

        return this.http.get(url)
            .pipe(map(response => <any>response),
                map(response => <TagType>response.items[0]),
                catchError(err => this.handleError(err)));

    }

    getTagDetails(uid: string): Observable<any> {
        let url = `api/v2/tags/${uid}/details`;
        return this.http.get(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err)));
    }
}