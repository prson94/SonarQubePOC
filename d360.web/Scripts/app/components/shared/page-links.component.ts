import { Component, EventEmitter, Output } from '@angular/core';


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

    links: any[] = [];

    constructor() {
        
        
    }

    ngOnInit() {
        
    }

    hasLinks() {
        return false;
    }
    
};
