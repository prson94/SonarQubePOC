import { Injectable } from '@angular/core';
import {Subject} from 'rxjs';
import { SecondaryNavItem, DynamicButton, AssetAction, SecondaryNavCurrentObject, SecondaryNavState, NavState } from '../models/secondaryNav.model';

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
    private rebuildHeaderSource = new Subject<any>();
    private secondaryNavState: SecondaryNavState;
    constructor() {
        this.secondaryNavState = new SecondaryNavState();
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


    setCurrentArea(area: string, icon: string, title: string) {
        this.currentAreaSource.next({ title: area, icon: icon, tabTitle: title });
        this.secondaryNavState.currentState.currentArea = { title: area, icon: icon, tabTitle: title };
        this.saveSecondaryNavState(this.secondaryNavState);
    }

    setCurrentObject(currentObject: SecondaryNavCurrentObject) {
        this.secondaryNavState.currentState.currentObject = currentObject;
        this.saveSecondaryNavState(this.secondaryNavState);
        this.currentObjectSource.next(currentObject);
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
    getLocalActiveItem(): SecondaryNavItem {
        return JSON.parse(localStorage.getItem('SecondaryNavState')).currentState.currentTab;
    }
    setLocalCurrentTabs(items: SecondaryNavItem[]) {
        this.secondaryNavState.currentState.shownTabs = items;
        this.saveSecondaryNavState(this.secondaryNavState);
    }
    getLocalCurrentTabs(): SecondaryNavItem[] {
        let state: SecondaryNavState = JSON.parse(localStorage.getItem('SecondaryNavState'));
        return state.currentState.shownTabs;
    }
    getLocalCurrentObject() {
        return JSON.parse(localStorage.getItem('SecondaryNavState')).currentState.currentObject;
    }
    getLocalCurrentArea() {
        return JSON.parse(localStorage.getItem('SecondaryNavState')).currentState.currentArea;
    }
    setLocalHomeUrl(url: string): any {
        this.secondaryNavState.currentState.currentHome = url;
        this.saveSecondaryNavState(this.secondaryNavState);
    }
    getLocalHomeUrl(): string {
        return JSON.parse(localStorage.getItem('SecondaryNavState')).currentState.currentHome;
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
    getCurrentState(): SecondaryNavState{
        return JSON.parse(localStorage.getItem('SecondaryNavState'));
    }

    private saveSecondaryNavState(state: SecondaryNavState) {
        localStorage.setItem('SecondaryNavState', JSON.stringify({ ...state }));
    }
}