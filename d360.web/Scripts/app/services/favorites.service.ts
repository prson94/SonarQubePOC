import { Observable, Subject, forkJoin } from "rxjs";
import { catchError, map, shareReplay, takeUntil, tap } from "rxjs/operators";
import { HttpClient, HttpContext } from "@angular/common/http";
import { Injectable } from '@angular/core';

import { FavoriteApiModel, FavoriteViewModel, HomepageAndFavoritesModel } from '../models/favorite.model';
import { JsonResult } from '../models/jsonresult.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { ROUTE_INDEPENDENT_QUERY } from "../http-interceptors";

@Injectable({
    providedIn: 'root'
})
export class FavoritesService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getFavorites(): Observable<FavoriteViewModel[]> {
        return this
            .http
            .get(
                'api/v2/membership/users/me/favorites',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => <FavoriteApiModel[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteCurrentUsersFavoritesV2(favoriteIds: number[]): Observable<JsonResult> {
        return this
            .http
            .delete('api/v2/membership/users/me/favorites/bulk', {
                body: favoriteIds
            })
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err)),
                tap(res => this.clearCache())
            );
    }

    toggleFavoriteV2(favorite: FavoriteApiModel) {
        return this
            .http
            .put(`api/v2/membership/users/me/favorites`, favorite)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err)),
                tap(res => this.clearCache())
            );
    }

    GetHomePage(): Observable<FavoriteApiModel> {
        return this.http
            .get(
                'api/v2/membership/users/me/getHomePage',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    toggleHomePageV2(homepage: FavoriteApiModel) {
        return this
            .http
            .put(`api/v2/membership/users/me/homepage`, homepage)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err)),
                tap(res => this.clearCache())
            )
    }

    //Observable for caching Favorites and Homepage
    private homefavoritecache$: Observable<HomepageAndFavoritesModel>;
    //Subject used to control when the cache is complete
    private reload$ = new Subject<void>();

    //Public method that creates, if needed, and gets the cached Observable
    public getHomePageAndFavorites(): Observable<HomepageAndFavoritesModel> {
        if (!this.homefavoritecache$) {
            this.homefavoritecache$ = this.requestHomePageAndFavorites().pipe(takeUntil(this.reload$));
        }
        return this.homefavoritecache$;
    }

    //Private method that combines the Favorites and GetHomepage calls and pipes it into a shareReplay Observable
    private requestHomePageAndFavorites(): Observable<HomepageAndFavoritesModel> {
        let favResponse = this.getFavorites();
        let homeResponse = this.GetHomePage();
        return forkJoin([favResponse, homeResponse], (favRes, homeRes) => {
            let res = new HomepageAndFavoritesModel();
            res.Homepage = homeRes;
            res.Favorites = favRes;
            return res;
        }).pipe(shareReplay(1));
    }

    //Private message that clears Favorties and Homepage cache. Called by the toggle methods
    private clearCache() {
        this.reload$.next();
        this.homefavoritecache$ = null;
    }

    moveUp(
        route: string,
        admin: boolean = false
    ) {
        let m = {
            route: route,
            moveUp: true
        };

        return this
            .http
            .put(`navigation/movefavorite?admin=${admin}`, m)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    moveDown(
        route: string,
        admin: boolean = false
    ) {
        let m = {
            route: route,
            moveUp: false
        };

        return this
            .http
            .put(`navigation/movefavorite?admin=${admin}`, m)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }
}
