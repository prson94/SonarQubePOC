import { Injectable } from '@angular/core';
import {Subject} from 'rxjs';
import { RightSidebarItem, DynamicButton, AssetAction } from '../models/rightsidebar.model';

@Injectable()
export class RightSidebarService {
    // Observable sources
    private rightSidebarSource = new Subject<RightSidebarItem>();
    private rightSidebarClearSource = new Subject<boolean>();
    private rightSidebarClearButtonSource = new Subject<boolean>();
    private rightSidebarClickedSource = new Subject<RightSidebarItem>();
    private currentAreaSource = new Subject<any>();
    private currentObjectSource = new Subject<any>();
    private hideHeaderSource = new Subject<boolean>();
    private rightSidebarButtonSource = new Subject<DynamicButton>();
    private assetActionSource = new Subject<AssetAction>();


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

    setCurrentArea(area: string, icon: string, title: string) {
        this.currentAreaSource.next({ title: area, icon: icon, tabTitle:title });
    }

    setCurrentObject(objectType: string, objectTypeID: number, objectName: string, objectID: number, isType: boolean, hasWorkFlow?: boolean) {
        this.currentObjectSource.next({ objectType, objectTypeID, objectName, objectID, isType, hasWorkFlow: hasWorkFlow == undefined ? false : hasWorkFlow });
    }

    clearCurrentObject() {
        this.currentObjectSource.next(null);
    }

    showButton(button: DynamicButton) {
        this.rightSidebarButtonSource.next(button);
    }
    clearButtons() {
        this.rightSidebarClearButtonSource.next(true);
    }

    // Service message commands
    showItem(rightSidebarItem: RightSidebarItem) {
        this.rightSidebarSource.next(rightSidebarItem);
    }

    clearItems() {
        this.rightSidebarClearSource.next(true);
    }

    itemClicked(item: RightSidebarItem) {
        this.rightSidebarClickedSource.next(item);
    }
    showHeader(val: boolean) {
        this.hideHeaderSource.next(val);
    }

    setActionTitleItems(val: AssetAction) {
        this.assetActionSource.next(val);
    }
}