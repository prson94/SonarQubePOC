import { Component, EventEmitter, Output, Input } from '@angular/core';
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
    }
  `],
    template: ` <div class="item" (click)="itemClick.emit({item:item})"><i class="fa fa-share-alt" aria-hidden="true"></i> {{item.title}}</div>
              `    
})

export class RightSidebarItemComponent {
    @Output() itemClick = new EventEmitter();
    @Input() item: RightSidebarItem;

    constructor() {
        
    }
};
