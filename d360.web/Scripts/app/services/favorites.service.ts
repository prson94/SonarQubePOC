import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";
import {HttpClient} from "@angular/common/http";
import {Injectable} from '@angular/core';

import {FavoriteApiModel} from '../models/favorite.model';
import {JsonResult} from '../models/jsonresult.model';

import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class FavoritesService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getFavorites(adminOnly: boolean = false): Observable<FavoriteApiModel[]> {
        return this
            .http
            .get(`api/v2/membership/users/me/favorites`)
            .pipe(
                map(response => <FavoriteApiModel[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteCurrentUsersFavorites(): Observable<JsonResult> {
        return this
            .http
            .delete('api/v2/membership/users/me/favorites')
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    toggleFavorite(favorite: FavoriteApiModel) {
        return this
            .http
            .put(`api/v2/membership/users/me/favorites`, favorite)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
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
