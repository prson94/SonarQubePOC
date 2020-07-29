import { CommonModule } from '@angular/common';
import { NgModule, Input, Component, Output, EventEmitter } from '@angular/core';
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
    selector: 'simple-accordion',
    template: `
        <div class="ui-accordion ui-widget" [@state]="state">
            <div class="ui-accordion-header ui-state-default" (click)="toggleActive();" [ngClass]="{'ui-state-active': active,'ui-state-hover':hover}" (mouseenter)="hover=true" (mouseleave)="hover=false">
                <span *ngIf="active" style="float:right;"><i class="fa fa-chevron-up"></i></span>
                <span *ngIf="!active" style="float:right;"><i class="fa fa-chevron-down"></i></span>                
                <div class="ui-accordion-header-info">
                    <a  (click)="null" style="text-decoration:none;">
                        <span class="elide-popup">
                            <span class="popup">{{header}}</span>
                            <span class="text">{{header}}</span>
                        </span>
                    </a>
                    <div *ngIf="tooltip" class="ig-input-label">
                        <div class="info-tip">
                            <i class="fa fa-question-circle"></i>
                            <div class="tip-container">
                                <div class="tooltip-content group">
                                    {{tooltip}}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div [style.display]="active ? 'block' : 'none'" style="margin:5px;">
                <ng-content></ng-content>
            </div>
        </div>
    `,
    animations: [
        trigger('state', [
            state('default', style({ opacity: '1' })),
            transition('* => void', [
                animate('500ms ease', style({ opacity: '0' })) 
            ])
        ])
    ]
})

export class SimpleAccordion {
    @Input() header: string = "";
    @Input() active: boolean = false;
    @Input() tooltip: string = "";

    @Output() activeChange = new EventEmitter();

    state = 'default';
    public hover: boolean = false;

    toggleActive() {
        this.active = !this.active;
        this.activeChange.emit(this.active);
    }
}

@NgModule({
    declarations: [
        SimpleAccordion,
    ],
    exports: [
        SimpleAccordion,
    ]
    , imports: [
        CommonModule,
    ]
})

export class SimpleAccordionModule { }