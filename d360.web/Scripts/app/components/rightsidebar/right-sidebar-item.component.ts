import { Component, EventEmitter, Output, Input, ChangeDetectionStrategy } from '@angular/core';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-right-sidebar-item',    
    template: ` <div class="right-side-item row" (click)="active=!active;activeChange.emit(active);" [ngClass]="{'right-side-active':active}">
                    <div class="row s12 center-align"><i class="fa" [ngClass]="{'fa-times':active, 'fa-share-alt':!active}"></i></div>                    
                    <div class="row s12 center-align"><span *ngIf="!active">{{title}}</span><span *ngIf="active">Close</span></div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class RightSidebarItemComponent {
    @Output() activeChange = new EventEmitter();
    @Input() active: boolean;

    @Input() title: string;    
};
