///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { HeaderActionsService } from '../../services/header-actions.service';

import { Subscription }   from 'rxjs/Subscription';

@Component({
    selector: 'd3s-header-actions',
    templateUrl: 'Navigation/HeaderActions'
})

export class HeaderActionsComponent {
    public hasHelp: boolean = true;
    public hasSearch: boolean = true;
    public hasNotifications: boolean = true;
    public hasRaiseIssue: boolean = false;

    subscription: Subscription;

    constructor(private headerActionsService: HeaderActionsService) {        
      /*  this.subscription = headerActionsService.hasNotifications$.subscribe(
            show => {                
                this.hasNotifications = show;
            })

        this.subscription = headerActionsService.hasRaiseIssueSource$.subscribe(
            show => {
                this.hasRaiseIssue = show;
            })

        this.subscription = headerActionsService.hasSearchSource$.subscribe(
            show => {
                this.hasSearch = show;
            })

        this.subscription = headerActionsService.hasHelp$.subscribe(
            show => {
                this.hasHelp = show;
            })*/
    }

    ngOnDestroy() {
        // prevent memory leak when component destroyed
     //   this.subscription.unsubscribe();
    }

}

