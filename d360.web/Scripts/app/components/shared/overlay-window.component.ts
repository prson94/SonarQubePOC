import { Component, Input, Output, EventEmitter, NgModule, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS } from '@angular/common/http'; 


@Component({
    selector: 'd3s-overlay-window',
    template: `
<div *ngIf="visible" class="container"
    [style.left]="(width >= 0) ? '-' + width + 'px' : null" 
    [style.width]="width + 'px'" 
    [style.height]="(height >= 0) ? height + 'px' : null" 
    [style.max-height.px]="maxHeight" 
    [style.max-width]="maxWidth + 'px'" 
    [style.padding]="padding + 'px'"
    [style.overflow-y]="overflowScroll">
    <header style="background-color: #f9f9f9 !important; min-height: 2em">
        {{headerText}}
        <span *ngIf="hasCloseButton" style="float:right;cursor: pointer; margin-right: 7px; font-size:1.1em;"><a style="color:#000;" (click)="visibleChange.emit(!visible)"><i class='fa fa-close'></i></a></span>
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
        z-index: 899;
        border: 1px solid #ccc;
        /*resize: both;*/
}
`
    ]
})

export class OverlayWindowComponent implements OnChanges {
    @Input() maxWidth: number = Infinity;
    @Input() maxHeight: number = Infinity;
    @Input() width: number = -1;
    @Input() height: number = -1;
    @Input() hasCloseButton: boolean = true;
    @Input() padding: number = 15;
    @Input() headerText: string = '';
    @Input() overflowScroll: string = 'auto';
    @Input() visible: boolean = true;
    @Input() draggable: boolean = false;
    @Output() visibleChange = new EventEmitter();

    constructor() { }

    ngOnChanges() {

    }

    onDrag(e: any) {
        //console.log(e);
    }

    onDrop(e: any) {
        //console.log(e);
    }

}

@NgModule({
    declarations: [
        OverlayWindowComponent
    ],
    exports: [
        OverlayWindowComponent
    ]
    , imports: [
        CommonModule,
    ],
    providers: [

    ]
})
export class D3SOverlayWindowModule { }