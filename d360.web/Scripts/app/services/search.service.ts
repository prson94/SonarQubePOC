import { Injectable } from '@angular/core';
import { SearchResultsObject, SearchQuery, SearchResultInfo } from '../models/search-result.model';
import { HttpClient } from '@angular/common/http';
import { catchError, map, takeUntil, shareReplay } from 'rxjs/operators';
import { Observable, Subject, of } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { SettingsHelper, SearchType } from '../models/settings.model';

@Injectable()
export class SearchService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    private getEmptyResult(): SearchResultsObject {
        let result = new SearchResultsObject();
        result.Categories = [];
        result.Result = new SearchResultInfo();
        result.Result.Results = [];
        result.Result.ElapsedMS = 0;
        result.Result.Matches = 0;
        return result;
    }

    getSearchResultsByQuery(query: SearchQuery): Observable<SearchResultsObject> {
        if (query.Term == undefined || query.Term == "") {
            //No search term, no results, no need to call endpoint
            return of(this.getEmptyResult());
        }

        return this.http
            .post('search/results', query)
            .pipe(
                map(res => <SearchResultsObject>res),
                catchError(err => this.handleError(err))
            );
    }

    public getSearchCategories(settings: any, showUsers: boolean = true, keepNotVisible: boolean = false): Observable<SearchType[]> {
        let exclude: string[] = [];
        if (settings) {
            if (settings['FusionEnabled'].toString() === 'false') {
                exclude.push('FusionAttributes');
                exclude.push('FusionType');
            }
            if (+settings.LineageVersion != 3)
                exclude.push('TechnicalAsset');
        }
        if (!showUsers) {
            exclude.push('Group');
            exclude.push('User');
        }
        let categories: SearchType[] = SettingsHelper.getSearchTypesList().filter(t => exclude.indexOf(t.value) == -1);

        return this.getVisibleCategories().pipe(
            map(res => categories.map(c => {
                c.visible = res.indexOf(c.value) >= 0;
                return c;
            }).filter(c => keepNotVisible || c.visible))
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
        return this.http.get('search/categories').pipe(
            map(res => <string[]>res),
            catchError(err => this.handleError(err)),
            shareReplay(1)
        );
    }

    //Private message that clears visible categories cache.
    private clearCache() {
        this.reload$.next();
        this.visibleCategories$ = null;
    }
}