import { Injectable } from '@angular/core';
import { SearchResults, SearchQuery } from '../models/search-result.model';
import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { catchError, map, takeUntil, shareReplay, delay } from 'rxjs/operators';
import { Observable, Subject, of, throwError } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { SettingsHelper, SearchType } from '../models/settings.model';
import { IndexableType, IndexableStatus } from "../models/search-admin.model";
import { FeatureFlags, FeatureFlagsService } from './featureflags.service';
import { ROUTE_INDEPENDENT_QUERY } from '../http-interceptors';
import { Table } from 'primeng/table';
import { FilterMatchMode } from 'primeng/api';

@Injectable({
    providedIn: 'root'
})
export class SearchService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService, private featureFlagService: FeatureFlagsService) { super(messagesService); }

    public getEmptyResult(): SearchResults {
        let result = new SearchResults();
        result.Results = [];
        result.Aggregations = { category: []};
        result.Matches = 0;
        result.ElapsedMS = { Query: 0, Augment: 0 };
        return result;
    }

    serachTableLocally(table: Table, searchString: string): void {
        if(searchString.startsWith('*') && !searchString.endsWith('*')) {
            const refinedSearchString = searchString.replace(/^\*/i, '');
            table.filterGlobal(refinedSearchString, FilterMatchMode.ENDS_WITH);
        } else if (!searchString.startsWith('*') && searchString.endsWith('*')) {
            const refinedSearchString = searchString.replace(/\*$/i, '');
            table.filterGlobal(refinedSearchString, FilterMatchMode.STARTS_WITH);
        } else {
            const refinedSearchString =  searchString.replace(/(^\*|\*$)/ig, '');
            table.filterGlobal(refinedSearchString, FilterMatchMode.CONTAINS);
        }
    }

    getSearchResultsByQuery(query: SearchQuery): Observable<SearchResults> {
        if (typeof query.Term === "undefined" || query.Term === "") {
            //No search term, no results, no need to call endpoint
            return of(this.getEmptyResult());
        }

        return this.http
            .post('api/v2/search/results', query)
            .pipe(
                map((res) => <SearchResults>res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        if (err.error.title === "Cannot connect to the search server") {
                            return throwError("ConnectionError");
                        }
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return of(this.getEmptyResult());
                })
            );
    }

    public downloadSearchExcel(query: SearchQuery): Observable<any> {
        return this.
            http
            .post("api/v2/search/results", query, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' });
    }

    public getSearchCategories(showUsers: boolean = true, keepNotVisible: boolean = false): Observable<SearchType[]> {
        let exclude: string[] = [];
        if (!showUsers) {
            exclude.push('Group');
            exclude.push('User');
        }
        let categories: SearchType[] = SettingsHelper.getSearchTypesList().filter((t) => exclude.indexOf(t.value) === -1);
        
        if (!this.featureFlagService.flags[FeatureFlags.SemanticTypesUiFlag]) {
            categories = categories.filter((s) => s.value !== "SemanticType");
        }
        
        return this.getVisibleCategories().pipe(
            map((res) => categories.map((c) => {
                c.visible = res.indexOf(c.value) >= 0;
                return c;
            }).filter((c) => keepNotVisible || c.visible))
        );
    }

    //Observable for caching visible Categories
    private visibleCategories$: Observable<string[]>;
    //Subject used to control when the cache is complete
    private reload$ = new Subject<void>();

    //Public method that creates, if needed, and gets the cached Observable
    public getVisibleCategories(): Observable<string[]> {
        if (!this.visibleCategories$) {
            this.visibleCategories$ = this.requestVisibleCategories().pipe(takeUntil(this.reload$));
        }
        return this.visibleCategories$;
    }

    //Private method that calls and pipes it into a shareReplay Observable
    private requestVisibleCategories(): Observable<string[]> {
        return this.http
            .get(
                'api/v2/search/categories', 
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            ).pipe(
            map((res) => <string[]>res),
            catchError((err) => this.handleError(err)),
            shareReplay(1)
        );
    }

    //Private message that clears visible categories cache.
    private clearCache() {
        this.reload$.next();
        this.visibleCategories$ = null;
    }

    public GetIndexableTypes(): Observable<IndexableType[]> {
        return this.http
            .get("api/v2/search/indexableTypes")
            .pipe(
                map((res) => <IndexableType[]>res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return [];
                })
            );
    }

    public GetIndexbleStatus(): Observable<IndexableStatus[]> {
        return this.http
            .get("api/v2/search/indexableStatus")
            .pipe(
                map((res) => <IndexableStatus[]>res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return [];
                })
            );
    }

    public SendRebildRequest(Class: number, assettypeuid: string) {
        let url = `api/v2/search/rebuild/${Class}/${assettypeuid}`;
        return this.http
            .post(url, "")
            .pipe(
                delay(1000),
                map((res) => res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return [];
                })
            );
    }
}