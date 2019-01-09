import { Injectable, EventEmitter } from '@angular/core';
import { Http } from '@angular/http';
import { Subject } from 'rxjs';
import { Favorite } from '../models/favorite.model';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class FavoritesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFavorites(adminOnly: boolean = false): Promise<Favorite[]> {        
        return this.http.get(`navigation/getfavorites?adminOnly=${adminOnly}`)
            .toPromise()
            .then(response => <Favorite[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteCurrentUsersFavorites(): Promise<JsonResult> {
        return this.http.delete('navigation/deletemyfavorites')
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    toggleFavorite(favorite: Favorite) {        
        return this.http.put(`navigation/togglefavorite`, favorite)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    
    moveUp(route: string, admin: boolean = false) {
        let m = {
            route: route,
            moveUp: true
        };

        return this.http.put(`navigation/movefavorite?admin=${admin}`, m)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    moveDown(route: string, admin: boolean = false) {
        let m = {
            route: route,
            moveUp: false
        };

        return this.http.put(`navigation/movefavorite?admin=${admin}`, m)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}