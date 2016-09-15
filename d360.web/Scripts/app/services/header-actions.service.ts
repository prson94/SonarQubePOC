///<reference path="../es6-shim.d.ts"/>
import { Injectable, EventEmitter } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { Favorite } from '../models/favorite.model';

@Injectable()
export class HeaderActionsService {    
    showFavorite: boolean = true;
    showNotifications: boolean = false;
    showHelp: boolean = false;
    showSearch: boolean = true;
    showRaiseIssue: boolean = false;  
    showFollow: boolean = true;

    // Observable sources
    private onFavoritesChangeSource = new Subject<Favorite[]>();
    public onFavoritesChanges$ = this.onFavoritesChangeSource.asObservable();

    private onSiteNavChangeSource = new Subject();
    public onSiteNavChanges$ = this.onSiteNavChangeSource.asObservable();

    emitFavoritesChange(favorites: Favorite[]) {
        this.onFavoritesChangeSource.next(favorites);
    }

    emitSiteNavChange() {
        this.onSiteNavChangeSource.next();
    }

}