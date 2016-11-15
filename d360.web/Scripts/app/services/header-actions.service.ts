import { Injectable, EventEmitter } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { Favorite } from '../models/favorite.model';

@Injectable()
export class HeaderActionsService {    
    showFavorite: boolean = true;
    showNotifications: boolean = false;
    showLegacy: boolean = true;
    showHelp: boolean = true;
    showSearch: boolean = true;
    showRaiseIssue: boolean = false;  
    showFollow: boolean = true;

    // Observable sources
    private onFavoritesChangeSource = new Subject();
    public onFavoritesChanges$ = this.onFavoritesChangeSource.asObservable();


    private onSiteNavChangeSource = new Subject();
    public onSiteNavChanges$ = this.onSiteNavChangeSource.asObservable();

    emitFavoritesChange() {
        this.onFavoritesChangeSource.next();
    }

    emitSiteNavChange() {
        this.onSiteNavChangeSource.next();
    }
}