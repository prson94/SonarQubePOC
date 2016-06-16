///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';

import {Subject} from 'rxjs/Subject';

@Injectable()
export class HeaderActionsService {
    //public hasNotifications$: Observable<boolean>;    
    // Observable sources
    showNotifications: boolean = true;
    showHelp: boolean = true;
    showSearch: boolean = true;
    showRaiseIssue: boolean = false;
   /* private hasNotificationsSource = new Subject<boolean>();
    private hasHelpSource = new Subject<boolean>();
    private hasSearchSource = new Subject<boolean>();
    private hasRaiseIssueSource = new Subject<boolean>();
    
    // Observable streams
    hasNotifications$ = this.hasNotificationsSource.asObservable();
    hasHelp$ = this.hasHelpSource.asObservable();
    hasSearchSource$ = this.hasSearchSource.asObservable();
    hasRaiseIssueSource$ = this.hasRaiseIssueSource.asObservable();
        

    // Service message commands
    showNotifications(show: boolean) {        
        this.hasNotificationsSource.next(show)
    }

    showHelp(show: boolean) {
        this.hasHelpSource.next(show)
    }

    showSearch(show: boolean) {
        this.hasSearchSource.next(show)
    }

    showRaiseIssue(show: boolean) {
        this.hasRaiseIssueSource.next(show)
    }*/
}