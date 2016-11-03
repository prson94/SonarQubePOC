import { Component, Input, ElementRef, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RightSidebarService  } from '../../services/index';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { Subscription }   from 'rxjs/Subscription';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-right-sidebar',      
    template: ` <div *ngIf="items && items.length > 0" class="hide-on-small-only right-sidebar">                
                    <div *ngFor="let item of items">
                        <d3s-right-sidebar-item [active]="item.active" (activeChange)="item.active=$event;itemClicked(item)" [title]="item.title"></d3s-right-sidebar-item>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class RightSidebarComponent {    
    subscription: Subscription;
    subscriptionClear: Subscription;
    items: RightSidebarItem[];  
  
    constructor(private rightSidebarService: RightSidebarService, ref: ChangeDetectorRef) {        
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
    
    itemClicked(item) {   
        if (item.active) {
            //look for any other already active items and fire click for them
            for (let ritem of this.items) {
                if (ritem.active && ritem.title != item.title) {
                    this.rightSidebarService.itemClicked(ritem);
                    ritem.active = false;
                }
            }
        }
        this.rightSidebarService.itemClicked(item);
    }     
};
