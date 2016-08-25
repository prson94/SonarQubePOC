///<reference path="../es6-shim.d.ts"/>
import { Injectable, EventEmitter } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { Favorite } from '../models/favorite.model';

@Injectable()
export class HeaderActionsService {    
    showFavorite: boolean = true;
    showNotifications: boolean = true;
    showHelp: boolean = true;
    showSearch: boolean = true;
    showRaiseIssue: boolean = false;  

    // Observable sources
    private onFavoritesChangeSource = new Subject<Favorite[]>();
    public onFavoritesChanges$ = this.onFavoritesChangeSource.asObservable();

    emitFavoritesChange(favorites: Favorite[]) {
        this.onFavoritesChangeSource.next(favorites);
    }

}