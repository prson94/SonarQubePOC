///<reference path="../es6-shim.d.ts"/>
import { Injectable, EventEmitter } from '@angular/core';
import { Http } from '@angular/http';
import { Subject } from 'rxjs/Subject';
import { Favorite } from '../models/favorite.model';
import { BaseService } from './base.service';
import { MessagesService } from './index';

@Injectable()
export class FavoritesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    // Observable sources
    private onFavoritesChangeSource = new Subject<string>();
    public onFavoritesChanges$ = this.onFavoritesChangeSource.asObservable();

    emitFavoritesChange(uri: string) {
        this.onFavoritesChangeSource.next(uri);
    }


    getFavorites(): Promise<Favorite[]> {
        return this.http.get('navigation/getfavorites')
            .toPromise()
            .then(response => <Favorite[]>response.json())
            .catch(err => this.handleError(err));

    }

    toggleFavorite(name: string, route: string) {
        let f = new Favorite();
        f.Name = name;
        f.Route = route;
        return this.http.put('navigation/togglefavorite', f)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }


}