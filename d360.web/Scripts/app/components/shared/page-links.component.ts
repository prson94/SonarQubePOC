import { Component, EventEmitter, Output } from '@angular/core';
import { RightSidebarService  } from '../../services/index';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { Subscription }   from 'rxjs/Subscription';

@Component({
    selector: 'd3s-page-links',
    template: `                 
                <div *ngIf="hasLinks()" class="right hide-on-med-and-down" (click)="onSideBarActivated.emit();">
                    <div><i class="fa fa-ellipsis-h" aria-hidden="true"></i></div>
                </div>
              `    
})

export class PageLinksComponent {    
    @Output() onSideBarActivated = new EventEmitter();
    
    subscription: Subscription;
    items: RightSidebarItem[];

    constructor(private rightSidebarService: RightSidebarService) {
        this.items = [];
        this.subscription = rightSidebarService.rightSidebar$.subscribe(
            item => {
                this.items.push(item);                
            });
        this.subscription = rightSidebarService.rightSidebarClear$.subscribe(
            item => {
                this.items.splice(0, this.items.length);
            })
    }

    ngOnDestroy() {
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
    }
    
    hasLinks() {
        return this.items && this.items.length > 0;
    }
    
};
