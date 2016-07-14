import { Component, EventEmitter, Output, Input, OnChanges, SimpleChange } from '@angular/core';
import { RightSidebarItem } from '../../models/rightsidebar.model';


@Component({
    selector: 'd3s-right-sidebar-item',
    styles: [`
    .item {
        background-color:rgba(84,164,218,1);
        padding: 5px 20px;
        border-radius:4px;
        color:white;
        font-weight:bold;
        cursor:pointer;
        margin-bottom:10px;
    }
    .active {
        background-color:#D32F2F;
    }
  `],
    template: ` <div *ngIf="!item.active" class="item" (click)="item.active=!item.active;itemClick.emit({item:item})"><i class="fa fa-share-alt" aria-hidden="true"></i> {{item.title}}</div>
                <div *ngIf="item.active" class="item active" (click)="item.active=!item.active;itemClick.emit({item:item})"><i class="fa fa-times" aria-hidden="true"></i> Close</div>
              `    
})

export class RightSidebarItemComponent {
    @Output() itemClick = new EventEmitter();
    @Input() item: RightSidebarItem;
        
    constructor() {
        
    }
    
};
