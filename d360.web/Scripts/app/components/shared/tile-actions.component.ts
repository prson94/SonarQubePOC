///<reference path="../../es6-shim.d.ts"/>
import {Component, EventEmitter, Output, Input} from '@angular/core';


@Component({
    selector: 'd3s-tile-actions',
    template: `
                <div id="FieldsTile_tools" class="TileTools">
                    <a *ngIf="hasAdd" class="btn btn-floating waves-effect waves-light brown lighten-1" (click)="addBtnClick()">
                        <i class="fa fa-plus" title="Add template"></i>
                    </a>
                </div>          
                `
})

export class TileActionsComponent {
    @Output() addClick = new EventEmitter();
    @Input() hasAdd: boolean;

    addBtnClick() {        
        this.addClick.emit(null);
    }
}