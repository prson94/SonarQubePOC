import { Component, Input, Output, EventEmitter } from '@angular/core';


@Component({
    selector: 'd3s-overlay-window',
    template: `
<div *ngIf="visible" class="container" 
    [style.left]="(width >= 0) ? '-' + width + 'px' : null" 
    [style.width]="width + 'px'" 
    [style.height]="(height >= 0) ? height + 'px' : null" 
    [style.max-height]="maxHeight + 'px'" 
    [style.max-width]="maxWidth + 'px'" 
    [style.padding]="padding + 'px'"
    [style.overflow-y]="overflowScroll ? 'auto' : 'initial'">
    <header>
        {{headerText}}
        <span *ngIf="hasCloseButton" style="float:right;cursor: pointer"><a style="color:#000;" (click)="visibleChange.emit(!visible)"><i class='fa fa-close'></i></a></span>
    </header>

    <ng-content></ng-content>
</div>
`,
    styles: [
        `
    .container {
        background-color: #fff;
        position: absolute;
        top: 0;
        display: block;
        box-shadow: 2px 2px 7px 0px rgba(0,0,0,0.5);
        z-index: 999;
}
`
    ]
})

export class OverlayWindowComponent {
    @Input() maxWidth: number = 500;
    @Input() maxHeight: number = 400;
    @Input() width: number = -1;
    @Input() height: number = -1;
    @Input() hasCloseButton: boolean = true;
    @Input() padding: number = 15;
    @Input() headerText: string = '';
    @Input() overflowScroll: boolean = true;
    @Input() visible: boolean = true;
    @Output() visibleChange = new EventEmitter();

    constructor() { }

}
