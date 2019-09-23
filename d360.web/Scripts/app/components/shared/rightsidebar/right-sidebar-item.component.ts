import { Component, EventEmitter, Output, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'd3s-right-sidebar-item',    
    template: ` <div class="right-side-item row center-align" (click)="active=!active;activeChange.emit(active);" [ngClass]="{'right-side-active':active}" [title]="title">                    
                    <i *ngIf="active" class="fa fa-times fa-lg"></i>
                    <ng-template [ngIf]="!active">
                        <i *ngIf="activeIcons.length==1" [class]="'fa fa-lg ' + activeIcons[0]"></i>    
                        <span *ngIf="activeIcons.length>1" class="fa-stack fa-lg">
                            <i [class]="'fa ' + activeIcons[0] + ' fa-stack-2x'"></i>
                            <i [class]="'fa ' +  activeIcons[1] + ' fa-stack-1x'"></i>
                        </span>
                    </ng-template>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class RightSidebarItemComponent {
    @Output() activeChange = new EventEmitter();
    @Input() active: boolean;

    @Input() title: string;    

    @Input() activeIcons: string[] = ["fa-share-alt"];

    @Input() url: string;
};
