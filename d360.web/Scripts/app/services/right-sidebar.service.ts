import { Injectable } from '@angular/core';
import {Subject} from 'rxjs';
import { RightSidebarItem } from '../models/rightsidebar.model';

@Injectable()
export class RightSidebarService {
    // Observable sources
    private rightSidebarSource = new Subject<RightSidebarItem>();
    private rightSidebarClearSource = new Subject<boolean>();
    private rightSidebarClickedSource = new Subject<RightSidebarItem>();
    private currentAreaSource = new Subject<any>();
    private hideHeaderSource = new Subject<boolean>();

    // Observable streams
    rightSidebar$ = this.rightSidebarSource.asObservable();
    rightSidebarClear$ = this.rightSidebarClearSource.asObservable();
    rightSidebarClicked$ = this.rightSidebarClickedSource.asObservable();
    currentArea$ = this.currentAreaSource.asObservable();
    hideHeader$ = this.hideHeaderSource.asObservable();

    setCurrentArea(area: string, icon: string) {
        this.currentAreaSource.next({ title: area, icon: icon });
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
    ShowHeader(val: boolean) {
        this.hideHeaderSource.next(val);
    }
}