import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { SecondaryNavItem, DynamicButton, AssetAction, SecondaryNavCurrentObject, SecondaryNavState, NavState } from '../models/secondaryNav.model';

import { SiteMenuService } from './site-menu.service';
import { PlatformLocation } from '@angular/common'
import { Router, NavigationEnd, NavigationStart, Params } from '@angular/router';


@Injectable({
    providedIn: 'root'
})
export class SecondaryNavService {
    activeTabTitle: string;
    artifactTypeId: number;
    // Observable sources
    private rightSidebarSource = new Subject<SecondaryNavItem>();
    private rightSidebarClearSource = new Subject<boolean>();
    private rightSidebarClearButtonSource = new Subject<boolean>();
    private rightSidebarClickedSource = new Subject<SecondaryNavItem>();
    private currentAreaSource = new Subject<any>();
    private currentObjectSource = new Subject<SecondaryNavCurrentObject>();
    private hideHeaderSource = new Subject<boolean>();
    private rightSidebarButtonSource = new Subject<DynamicButton>();
    private assetActionSource = new Subject<AssetAction>();
    private assetActionClearSource = new Subject<boolean>();
    private homeUrlChangeSource = new Subject<string>();
    private rebuildHeaderSource = new Subject<any>();
    private secondaryNavState: SecondaryNavState;
    private refreshStatsSource = new Subject<boolean>();
    private updateObjectSource = new Subject<any>();


    private crossNavURIS: string[] = [];

    constructor(private siteMenuService: SiteMenuService, location: PlatformLocation, private router: Router) {
        this.secondaryNavState = new SecondaryNavState();
        location.onPopState(() => {
            this.isSidebarCreated = false;
            this.invalidateKey();
        });

        router.events.subscribe((event) => {
            if (event instanceof NavigationStart) {
                if (this.router.url.indexOf("dashboard") != -1) {
                    this.isSidebarCreated = false;
                    this.invalidateKey();
                }
            }

            if (event instanceof NavigationEnd) {
                var homeUrl = this.secondaryNavState.currentState.currentHome ? this.secondaryNavState.currentState.currentHome : '';

                if (!this.crossNavURIS.some(x => x == homeUrl)) {
                    this.crossNavURIS.push(homeUrl);
                }
                if (!this.crossNavURIS.some(x => event.url.toLowerCase() == x.toLowerCase())) {
                    this.isSidebarCreated = false;
                    this.invalidateKey();
                }
            }
        });

    }

    public getCurrentUrl() {
        return this.router.url;
    }

    public setLoadedKey(_key: string) {
        localStorage.setItem('loadedNavItem', _key);
    }
    public invalidateKey() {
        localStorage.setItem('loadedNavItem', '{"AssetId":"","AssetTypeIdb":"","Uid":"","Object":"","ObjectId":""}');
    }
    public getLoadedKey(): string {
        return localStorage.getItem('loadedNavItem');
    }

    // Observable streams
    rightSidebar$ = this.rightSidebarSource.asObservable();
    rightSidebarClear$ = this.rightSidebarClearSource.asObservable();
    rightSidebarButtonClear$ = this.rightSidebarClearButtonSource.asObservable();
    rightSidebarClicked$ = this.rightSidebarClickedSource.asObservable();
    currentArea$ = this.currentAreaSource.asObservable();
    currentObject$ = this.currentObjectSource.asObservable();
    hideHeader$ = this.hideHeaderSource.asObservable();
    rightSidebarButton$ = this.rightSidebarButtonSource.asObservable();
    assetAction$ = this.assetActionSource.asObservable();
    assetActionClear$ = this.assetActionClearSource.asObservable();
    homeUrlChange$ = this.homeUrlChangeSource.asObservable();
    rebuildHeader$ = this.rebuildHeaderSource.asObservable();
    refreshStats$ = this.refreshStatsSource.asObservable();
    updateObject$ = this.updateObjectSource.asObservable();

    private isSidebarCreated: boolean = false;

    getSiteMenuService(): SiteMenuService {
        return this.siteMenuService;
    }

    setCurrentArea(area: string, icon: string, title: string) {
        this.currentAreaSource.next({ title: area, icon: icon, tabTitle: title });
        this.secondaryNavState.currentState.currentArea = { title: area, icon: icon, tabTitle: title };
        this.saveSecondaryNavState(this.secondaryNavState);
    }

    setCurrentObject(currentObject: SecondaryNavCurrentObject) {
        this.secondaryNavState.currentState.currentObject = currentObject;
        this.saveSecondaryNavState(this.secondaryNavState);
        this.currentObjectSource.next(currentObject);
        this.isSidebarCreated = true;
    }

    clearCurrentObject() {
        this.secondaryNavState.currentState.currentObject = undefined;
        this.saveSecondaryNavState(this.secondaryNavState);
        this.currentObjectSource.next(null);
    }

    showButton(button: DynamicButton) {
        this.rightSidebarButtonSource.next(button);
    }
    clearButtons() {
        this.rightSidebarClearButtonSource.next(true);
    }
    clearActions() {
        this.assetActionClearSource.next(true);
    }

    refreshStats() {
        this.refreshStatsSource.next(true);
    }

    // Service message commands
    showItem(rightSidebarItem: SecondaryNavItem) {
        if (rightSidebarItem && rightSidebarItem.url)
            this.crossNavURIS.push(rightSidebarItem.url);
        this.rightSidebarSource.next(rightSidebarItem);
    }

    clearItems() {
        this.crossNavURIS = [];
        this.rightSidebarClearSource.next(true);
    }

    itemClicked(item: SecondaryNavItem) {
        this.setLocalActiveItem(item);
        this.rightSidebarClickedSource.next(item);
    }
    clearLocalActiveItem() {
        localStorage.removeItem('SecondaryNav_CurrentTab');
    }
    showHeader(val: boolean) {
        if (!val)
            this.currentObjectSource.next(null);
        this.hideHeaderSource.next(val);
    }

    setActionTitleItems(val: AssetAction) {
        this.assetActionSource.next(val);
    }

    rebuildFromStorage(state: NavState) {
        this.secondaryNavState.currentState = state;
        this.saveSecondaryNavState(this.secondaryNavState);
        this.rebuildHeaderSource.next(true);
    }

    //local storage functions 
    setLocalActiveItem(item: SecondaryNavItem) {
        this.secondaryNavState.currentState.currentTab = item;
        this.saveSecondaryNavState(this.secondaryNavState);
    }
    
    setLocalCurrentTabs(items: SecondaryNavItem[]) {
        this.secondaryNavState.currentState.shownTabs = items;
        this.saveSecondaryNavState(this.secondaryNavState);
    }
    
    setLocalHomeUrl(url: string): any {
        this.homeUrlChangeSource.next(url);
        this.secondaryNavState.currentState.currentHome = url;
        this.saveSecondaryNavState(this.secondaryNavState);
    }

    getLocalHomeUrl(): string {
        return this.secondaryNavState.currentState.currentHome;
    }

    clearSecondaryNavLocalStorage() {
        localStorage.removeItem('SecondaryNavState');
    }
    saveLastState(): any {
        if (this.secondaryNavState.currentState && this.secondaryNavState.currentState.currentObject) {
            this.secondaryNavState.pushPreviousState({ ...this.secondaryNavState.currentState });
            this.saveSecondaryNavState(this.secondaryNavState);
        }
    }

    getItemState(url: string): NavState {
        let current = this.getCurrentState();
        return current.previousStates.find(state => state.currentTab && state.currentTab.url == url);
    }
    
    getCurrentState(): SecondaryNavState {
        return JSON.parse(localStorage.getItem('SecondaryNavState'));
    }

    private saveSecondaryNavState(state: SecondaryNavState) {
        localStorage.setItem('SecondaryNavState', JSON.stringify({ ...state }));
    }

    getArtifactTypeIdFromRouteParams(params: Params): number {
        let artifactTypeId: number;
        if (params.artifactTypeId) {
            artifactTypeId = Number(params.artifactTypeId);
        } else if (params.objectId) {
            artifactTypeId = Number(params.objectId);
        }
        return artifactTypeId;
    }

    resetSecondaryNavActiveItem(): void {
        this.resetSecondaryNavActiveTab();
        this.resetSecondaryNavActiveArtifact();
    }

    resetSecondaryNavActiveTab(): void {
        this.activeTabTitle = null;
    }

    resetSecondaryNavActiveArtifact(): void {
        this.artifactTypeId = null;
    }

    updateObject(key: string, value: any) {
        this.updateObjectSource.next({ key: key, value: value });
    }
}