///<reference path="../../es6-shim.d.ts"/>
import {Component, EventEmitter, Output, Input} from '@angular/core';


@Component({
    selector: 'd3s-tile-actions',
    styles: [`
    .btn-flat {
        padding:0;
    }
    
  `],
    template: `
                <div id="FieldsTile_tools" class="TileTools">
                    <a *ngIf="hasAdd" class="waves-effect waves-teal btn-flat" (click)="addBtnClick()">
                        <i class="fa fa-plus" [title]="addTitle"></i>
                    </a>
                </div>          
                `
})

export class TileActionsComponent {
    @Output() addClick = new EventEmitter();
    @Input() hasAdd: boolean;
    @Input() addTitle: string = "Add";

    addBtnClick() {        
        this.addClick.emit(null);
    }
}