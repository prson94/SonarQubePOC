import { Component, EventEmitter, Output, Input, OnChanges, SimpleChange } from '@angular/core';
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
        font-size:0.7em;
        cursor:pointer;
        margin-bottom:10px;
    }
    .active {
        background-color:#D32F2F;
    }
  `],
    template: ` <div *ngIf="!item.active" class="item row" (click)="item.active=!item.active;itemClick.emit({item:item})">
                    <div class="row s12 center-align"><i class="fa fa-share-alt" aria-hidden="true"></i></div>
                    <div class="row s12 center-align">{{item.title}}</div>
                </div>
                <div *ngIf="item.active" class="item active row" (click)="item.active=!item.active;itemClick.emit({item:item})">
                    <div class="row s12 center-align"><i class="fa fa-times" aria-hidden="true"></i></div>
                    <div class="row s12 center-align">Close</div>
                </div>
              `    
})

export class RightSidebarItemComponent {
    @Output() itemClick = new EventEmitter();
    @Input() item: RightSidebarItem;

        
    constructor() {   }
    
};
