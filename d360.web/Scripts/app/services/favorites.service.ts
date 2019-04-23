import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";
import {HttpClient} from "@angular/common/http";
import {Injectable} from '@angular/core';

import {Favorite} from '../models/favorite.model';
import {JsonResult} from '../models/jsonresult.model';

import {MessagesService} from './messages.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class FavoritesService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getFavorites(adminOnly: boolean = false): Observable<Favorite[]> {
        return this
            .http
            .get(`navigation/getfavorites?adminOnly=${adminOnly}`)
            .pipe(
                map(response => <Favorite[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteCurrentUsersFavorites(): Observable<JsonResult> {
        return this
            .http
            .delete('navigation/deletemyfavorites')
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    toggleFavorite(favorite: Favorite) {
        return this
            .http
            .put(`navigation/togglefavorite`, favorite)
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
