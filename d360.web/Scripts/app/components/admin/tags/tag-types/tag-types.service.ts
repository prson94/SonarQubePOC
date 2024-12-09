import { BehaviorSubject, Observable } from "rxjs";
import { HttpClient} from '@angular/common/http';
import { catchError, map,  } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { TagTypesViewModel } from "./tag-types.model";


@Injectable({
    providedIn: 'root'
})
export class TagTypesService {

    private _connectionError: BehaviorSubject<boolean> = new BehaviorSubject(false);

    constructor(
        private http: HttpClient,
    ) {

    }

    getAllTagTypes(): Observable<TagTypesViewModel[]> {
        return this
            .http
            .get(`api/v2/tags/tagTypes`)
            .pipe(
                map((response) => <TagTypesViewModel[]>response),
                catchError((err) => {
                    if (err === "ConnectionError") {
                        this._connectionError.next(true);
                    }
                    return ([]);
                }))
            ;
    }

    addNewTagType(tagType: string): Observable<TagTypesViewModel> {
        return this
                .http
                .post<TagTypesViewModel>('/api/v2/tags/tagTypes', { 'Value': tagType });
    }

}
