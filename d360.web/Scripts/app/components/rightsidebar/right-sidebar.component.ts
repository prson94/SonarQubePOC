import { Component, Input, ElementRef, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { RightSidebarService  } from '../../services/index';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { RightSidebarItemComponent } from './right-sidebar-item.component';
import { Subscription }   from 'rxjs/Subscription';

@Component({
    selector: 'd3s-right-sidebar',  
    styles: [`
    .sidebar{
        position:absolute;
        width:50px;
        right:5px; 
        
    }
  `],  
    template: ` <div *ngIf="items && items.length > 0" class="hide-on-small-only sidebar" [style.top]="calculatedTop()">                
                    <div *ngFor="let item of items">
                        <d3s-right-sidebar-item [item]="item" (itemClick)="itemClicked($event.item)"></d3s-right-sidebar-item>
                    </div>
                </div>
              `,
    directives: [RightSidebarItemComponent]
})

export class RightSidebarComponent implements OnChanges {
    @Input() visible: boolean = false;
    @Input() titleHeight: number = 0;
    
    @Output() visibleChange = new EventEmitter() // an event emitter
    
    subscription: Subscription;
    items: RightSidebarItem[];
    canHideTimeoutID: number = 0;
    canHide: boolean = false;

    constructor(private _eref: ElementRef, private rightSidebarService: RightSidebarService) {        
        this.items = [];
        this.subscription = rightSidebarService.rightSidebar$.subscribe(
            item => {
                this.items.push(item);
                if (this.items.length == 1) {
                    this.visible = true;
                    this.visibleChange.emit(this.visible);
                }
            });
        this.subscription = rightSidebarService.rightSidebarClear$.subscribe(
            item => {
                this.items.splice(0, this.items.length);
                this.visible = false;
                this.visibleChange.emit(this.visible);
            })
    }

    ngOnDestroy() {
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {    
        for (let p in changes) {
            if (p == 'visible') {
                if (this.canHideTimeoutID <= 0)                
                    this.canHideTimeoutID = window.setTimeout(() => this.allowHide(), 2000);
            }

        }
    }
    allowHide() {
        this.canHide = true;
        this.canHideTimeoutID = 0;
    }

    itemClicked(item) {
        //look for any other already active items and fire click for them
        for (let ritem of this.items) {
            if (ritem.active && ritem.title != item.title) {
                this.rightSidebarService.itemClicked(ritem);
                ritem.active = false;                
            }
        }
        this.rightSidebarService.itemClicked(item);
    }

    private calculatedTop(): string{
        
        if (this.titleHeight) {            
            return this.titleHeight + 45 + 'px';
        }
        return '45px';        
    }
    
    
};
