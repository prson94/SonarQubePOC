import { Injectable, EventEmitter } from '@angular/core';
import { Subject } from 'rxjs';
import { HeaderActions } from '../models/header.model';

declare var CompanySettings;

@Injectable()
export class HeaderActionsService {
    showFavorite: boolean = CompanySettings.ShowFavorites != 'false';
    showNotifications: boolean = false;
    showHelp: boolean = true;
    showSearch: boolean = true;
    showRaiseIssue: boolean = true;
    showFollow: boolean = CompanySettings.ShowImpactSidebar != 'false';
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

    public setActionsToDefaultValues() {
        this.showFavorite = CompanySettings.ShowFavorites != 'false';
        this.showNotifications = false;
        this.showHelp = true;
        this.showSearch = true;
        this.showRaiseIssue = true;
        this.showFollow = CompanySettings.ShowImpactSidebar != 'false';
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