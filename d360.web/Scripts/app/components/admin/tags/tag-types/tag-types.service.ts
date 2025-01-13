import { BehaviorSubject, Observable } from "rxjs";
import { HttpClient} from '@angular/common/http';
import { catchError, map,  } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { TagTypesViewModel } from "./tag-types.model";
import { BaseObservableService } from "../../../../services/baseObservable.service";
import { MessagesObservableService } from "../../../../services/messages-observable.service";


@Injectable({
    providedIn: 'root'
})
export class TagTypesService extends BaseObservableService {

    private _connectionError: BehaviorSubject<boolean> = new BehaviorSubject(false);

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getAllTagTypes(): Observable<TagTypesViewModel[]> {
        return this
            .http
            .get(`api/v2/tags/tagTypes`)
            .pipe(
                map((response) => <TagTypesViewModel[]>response),
                catchError((err) => this.handleError(err)));
    }

    addNewTagType(tagType: string): Observable<TagTypesViewModel> {
        return this
                .http
                .post<TagTypesViewModel>('/api/v2/tags/tagTypes', { 'Value': tagType })
                .pipe(
                            map((response) => {
                                return response;
                            }),
                            map((item) => { return <TagTypesViewModel>item; }),
                            catchError((err) => this.handleError(err)));
                ;
    }

    updateTagType(tagType: string, tagId: string): Observable<TagTypesViewModel> {
        return this
                .http
                .put<TagTypesViewModel>(`/api/v2/tags/tagTypes/${tagId}`, { 'Value': tagType });
    }

    deleteTagType(tagId: string): Observable<TagTypesViewModel> {
        return this
                .http
                .delete<TagTypesViewModel>(`/api/v2/tags/tagTypes/${tagId}`);
    }

}
