import { Component, EventEmitter, Output, Input, OnChanges, SimpleChange, trigger,state,style,transition,animate } from '@angular/core';
import { RightSidebarItem } from '../../models/rightsidebar.model';


@Component({
    selector: 'd3s-right-sidebar-item',
    styles: [`
    .item {
        background-color:rgba(84,164,218,1);
        padding: 2px 2px;
        border-radius:4px;
        color:white;
        font-weight:bold;
        font-size:0.9em;
        cursor:pointer;
        margin-bottom:10px;
    }
    .active {
        background-color:#D32F2F;
    }
  `],
    animations: [
        trigger('itemState', [
            state('inactive', style({                
                transform: 'rotate(-360deg)'
            })),
            state('active', style({                
                transform: 'rotate(0deg)'
            })),
            transition('inactive => active', animate('100ms ease-in')),
            transition('active => inactive', animate('100ms ease-out'))
        ])
    ],
    template: ` <div class="item active row" (click)="item.active=!item.active;itemClick.emit({item:item})" [ngClass]="{'active':item.active}">
                    <div class="row s12 center-align"><i class="fa" aria-hidden="true" [ngClass]="{'fa-times':item.active, 'fa-share-alt':!item.active}"></i></div>                    
                    <div class="row s12 center-align"><span *ngIf="!item.active">{{item.title}}</span><span *ngIf="item.active">Close</span></div>
                </div>
              `    
})

export class RightSidebarItemComponent {
    @Output() itemClick = new EventEmitter();
    @Input() item: RightSidebarItem;

    itemState(item) {
        return item.active ? "active" : "inactive";
    }
        
    constructor() {   }
    
};
