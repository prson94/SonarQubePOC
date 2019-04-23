import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {
    catchError,
    distinctUntilChanged,
    map,
    switchMap
} from 'rxjs/operators';
import {Observable} from 'rxjs';

import {LineageEditorModelV2} from '../models/lineage.model';

import {MessagesService} from './messages.service';

import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class LineageService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    public getLineageDiagram(
        type: string,
        id: number
    ): Observable<any> {
        const url = `api/v2/lineage/${type}/${id}`;

        return this.http.get(url).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    public postLineageDiagram(model: LineageEditorModelV2): Observable<any> {
        const url = `api/v2/lineage`;

        return this.http.post(url, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    public getLineageObjectTypes(): Observable<any> {
        const url = 'api/lineage/objectTypes';

        return this.http.get(url).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    public getLineageIntersectTypes(): Observable<any> {
        const url = 'api/lineage/intersectTypes';

        return this.http.get(url)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public queryObjectTypes(
        type: string,
        id: number,
        query: string
    ): Observable<any[]> {
        const url = `api/lineage/query/objects/${type}/${id}?query=${query}`;

        return this.http.get(url).pipe(
            map(response => <any[]>response)
        );
    }

    public getLineageObjects(event: Observable<any>) {
        let uri = `api/lineage/objects/`;

        return event.pipe(
            distinctUntilChanged(),
            switchMap(
                event => {
                    let uri = `api/lineage/objects/${event.assetTypeId}?offset=${event.event.first}&rows=${event.event.rows}`;

                    if (event.event.globalFilter != null && event.event.globalFilter.length > 0) {
                        uri += `&query=${event.event.globalFilter}`;
                    }

                    return this.http.get(uri).pipe(
                        map(res => res),
                        map(res => {
                                return {
                                    assetTypeId: event.assetTypeId,
                                    results: res,
                                    event: event.event
                                }
                            }
                        )
                    );
                }
            )
        );
    }

    public getLineageObjectDetail(
        type: string,
        id: number
    ): Observable<any> {
        const url = `resources/${type}/${id}/templates/tooltip/preview`;

        return this.http.get(url).pipe(
            map(() => {
            }),
            catchError(err => this.handleError(err))
        );
    }

    public getLineageNodeDataForObject(
        type: string,
        id: number
    ): Observable<any> {
        const url = `diagrams/${type}/${id}/lineagenode`;

        return this.http.get(url).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }
}
