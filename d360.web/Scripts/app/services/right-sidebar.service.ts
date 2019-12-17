import { Injectable } from '@angular/core';
import {Subject} from 'rxjs';
import { SecondaryNavItem, DynamicButton, AssetAction, SecondaryNavCurrentObject } from '../models/secondaryNav.model';

@Injectable()
export class SecondaryNavService {
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

    setCurrentArea(area: string, icon: string, title: string) {
        this.currentAreaSource.next({ title: area, icon: icon, tabTitle: title });
        localStorage.setItem('SecondaryNav_CurrentArea', JSON.stringify({ title: area, icon: icon, tabTitle: title }));
    }

    setCurrentObject(currentObject: SecondaryNavCurrentObject) {
        localStorage.setItem('SecondaryNav_CurrentObject', JSON.stringify(currentObject));
        this.currentObjectSource.next(currentObject);
    }

    clearCurrentObject() {
        localStorage.removeItem('SecondaryNav_CurrentObject')
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

    // Service message commands
    showItem(rightSidebarItem: SecondaryNavItem) {
        this.rightSidebarSource.next(rightSidebarItem);
    }

    clearItems() {
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
        this.hideHeaderSource.next(val);
    }

    setActionTitleItems(val: AssetAction) {
        this.assetActionSource.next(val);
    }

    //local storage functions 
    setLocalActiveItem(item: SecondaryNavItem) {
        localStorage.setItem('SecondaryNav_CurrentTab', JSON.stringify(item));
    }
    getLocalActiveItem(): SecondaryNavItem {
        return JSON.parse(localStorage.getItem('SecondaryNav_CurrentTab'));
    }
    setLocalCurrentTabs(items: SecondaryNavItem[]) {
        localStorage.setItem('SecondaryNav_ShownTabs', JSON.stringify(items));
    }
    getLocalCurrentTabs(): SecondaryNavItem[] {
        return JSON.parse(localStorage.getItem('SecondaryNav_ShownTabs'));
    }
    getLocalCurrentObject() {
        return JSON.parse(localStorage.getItem('SecondaryNav_CurrentObject'));
    }
    getLocalCurrentArea() {
        return JSON.parse(localStorage.getItem('SecondaryNav_CurrentArea'));
    }
    setLocalHomeUrl(url: string): any {
        localStorage.setItem('SecondaryNav_CurrentHome', url);
    }
    getLocalHomeUrl(): string {
        return localStorage.getItem('SecondaryNav_CurrentHome');
    }
    clearSecondaryNavLocalStorage() {
        localStorage.removeItem('SecondaryNav_CurrentTab');
        localStorage.removeItem('SecondaryNav_ShownTabs');
        localStorage.removeItem('SecondaryNav_CurrentObject');
        localStorage.removeItem('SecondaryNav_CurrentArea');
        localStorage.removeItem('SecondaryNav_CurrentHome');
    }
}