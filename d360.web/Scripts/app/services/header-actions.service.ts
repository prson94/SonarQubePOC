import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { HeaderActions } from '../models/header.model';

@Injectable()
export class HeaderActionsService {
    showFavorite: boolean = false;
    showNotifications: boolean = false;
    showHelp: boolean = true;
    showSearch: boolean = true;
    showRaiseIssue: boolean = true;
    showFollow: boolean = false;
    showShoppingCart: boolean = true;
    showHomePage: boolean = true;
    forceTakeActionHidden: boolean = false;

    private headerActionsSource = new Subject<HeaderActions>();
    public onHeaderActionsChange$ = this.headerActionsSource.asObservable();

    public setCurrentHeaderActions(actions: HeaderActions) {
        this.headerActionsSource.next(actions);
    }
    // Observable sources
    private onFavoritesChangeSource = new Subject();
    public onFavoritesChanges$ = this.onFavoritesChangeSource.asObservable();

    private onSiteNavChangeSource = new Subject();
    public onSiteNavChanges$ = this.onSiteNavChangeSource.asObservable();

    private onSiteCountsChangeSource = new Subject();
    public onSiteCountsChange = this.onSiteCountsChangeSource.asObservable();

    public setActionsToDefaultValues(showFavorite: boolean, showFollow: boolean) {
        this.showFavorite = showFavorite;
        this.showNotifications = false;
        this.showHelp = true;
        this.showSearch = true;
        this.showRaiseIssue = true;
        this.showFollow = showFollow;
        this.showShoppingCart = true;
        this.showHomePage = true;
        this.forceTakeActionHidden = false;
    }

    emitFavoritesChange() {
        this.onFavoritesChangeSource.next();
    }

    emitSiteNavChange() {
        this.onSiteNavChangeSource.next();
    }
    emitCountChange() {
        this.onSiteCountsChangeSource.next();
    }
}