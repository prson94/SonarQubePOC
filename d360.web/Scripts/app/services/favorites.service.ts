
import { Injectable, EventEmitter } from '@angular/core';
import { Http } from '@angular/http';
import { Subject } from 'rxjs/Subject';
import { Favorite } from '../models/favorite.model';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';

@Injectable()
export class FavoritesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFavorites(adminOnly: boolean = false): Promise<Favorite[]> {
        return this.http.get(`navigation/getfavorites?adminOnly=${adminOnly}`)
            .toPromise()
            .then(response => <Favorite[]>response.json())
            .catch(err => this.handleError(err));

    }

    toggleFavorite(name: string, route: string, admin: boolean = false) {
        let f = new Favorite();
        f.Name = name;
        f.Route = route;
        return this.http.put(`navigation/togglefavorite?admin=${admin}`, f)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    //toggleFavorite2(name: string, objectType: string, objectId: number, parentId: number = 0, admin: boolean = false) {
    //    let f = new Favorite();
    //    f.Name = name;
    //    f.ObjectID = objectId;
    //    if (parentId != null && parentId != 0)
    //        f.ParentID = parentId;
    //    f.ObjectType = objectType;
    //    return this.http.put(`navigation/togglefavorite?admin=${admin}`, f)
    //        .toPromise()
    //        .then(response => response.json())
    //        .catch(err => this.handleError(err));
    //}

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