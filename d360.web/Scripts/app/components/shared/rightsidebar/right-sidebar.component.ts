import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { RightSidebarService  } from '../../../services/right-sidebar.service';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { Subscription }   from 'rxjs';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-right-sidebar',      
    template: ` <div *ngIf="items && items.length > 0" class="hide-on-small-only right-sidebar">                
                    <div *ngFor="let item of items; trackBy: trackById">
                        <d3s-right-sidebar-item [active]="item.active" (activeChange)="item.active=$event;itemClicked(item)" [title]="item.title" [activeIcons]="item.icons"></d3s-right-sidebar-item>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class RightSidebarComponent {    
    subscription: Subscription;
    subscriptionClear: Subscription;
    items: RightSidebarItem[];  
    hostUrl: string;
  
    constructor(
        private rightSidebarService: RightSidebarService,
        ref: ChangeDetectorRef,
        private router: Router
    ) {        
        this.items = [];
        this.subscription = rightSidebarService.rightSidebar$.subscribe(
            item => {                                
                this.items.push(item);
                this.items = _.sortBy(this.items, 'title');                
                ref.markForCheck();
            });
        this.subscriptionClear = rightSidebarService.rightSidebarClear$.subscribe(
            item => {
                this.items.splice(0, this.items.length);                                
                ref.markForCheck();
            })
    }

    ngOnDestroy() {        
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
        this.subscriptionClear.unsubscribe();
    }

    trackById(index, item) {        
        return item.tag;
    }
    
    itemClicked(item: RightSidebarItem) {   
        if (item.active) {
            //look for any other already active items and fire click for them
            let isFirstItemOpen = true;
            for (let ritem of this.items) {                
                if (ritem.active && ritem.title != item.title) {
                    this.rightSidebarService.itemClicked(ritem);
                    ritem.active = false;
                    isFirstItemOpen = false;                     
                }
            }            
            if (isFirstItemOpen) this.hostUrl = this.router.url;            
            this.rightSidebarService.itemClicked(item);
            if (item.hasDynamicUrl) this.router.navigateByUrl(item.dynamicUrlCallback());
            else if (item.url) this.router.navigateByUrl(item.url);
        }        
        else {
            //return to previous url if the item is a url otherwise fire click event            
            if (item.url)
                this.router.navigateByUrl(this.hostUrl);
            else
                this.rightSidebarService.itemClicked(item);
        }        
    }     
};
