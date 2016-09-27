
import { Input, Component, OnInit, Output, EventEmitter, transition, animate, style, trigger, state } from '@angular/core';

@Component({
    selector: 'simple-accordion',
    template: `
        <div class="ui-accordion ui-widget" [@state]="state">
            <div class="ui-accordion-header ui-state-default" style="/*width:100%;border-bottom:2px solid black;cursor:pointer;*/" (click)="toggleActive();" [ngClass]="{'ui-state-active': active,'ui-state-hover':hover}" (mouseenter)="hover=true" (mouseleave)="hover=false">
                <span *ngIf="active" style="float:right;"><i class="fa fa-chevron-up"></i></span>
                <span *ngIf="!active" style="float:right;"><i class="fa fa-chevron-down"></i></span>                
                <a  (click)="null" style="text-decoration:none;">{{header}}</a>
            </div>
            <div [style.display]="active ? 'block' : 'none'" style="margin:5px;">
                <ng-content></ng-content>
            </div>
        </div>
    `,
    animations: [
        trigger('state', [
            state('default', style({opacity: '1' })),
            transition('* => void', [
                animate('500ms ease', style({ opacity: '0' }))
            ])
        ])
       ]
})

export class SimpleAccordion implements OnInit {
    @Input() header: string = "";
    @Input() active: boolean = false;
    @Output() activeChange = new EventEmitter();

    state = 'default';


    constructor() {
    }

    ngOnInit() {

    }

    toggleActive() {
        this.active = !this.active;
        this.activeChange.emit(this.active);
    }

}

