///<reference path="../../es6-shim.d.ts"/>
import {Component, EventEmitter, Output, Input} from '@angular/core';


@Component({
    selector: 'd3s-tile-actions',
    styles: [`
    .btn-flat {
        padding:0;
        margin-left:10px;
    }
    .spacer {
        
    }
  `],
    template: `
                <div id="FieldsTile_tools" class="TileTools">
                    <a *ngIf="hasAdd" class="waves-effect waves-teal btn-flat" (click)="addClick.emit(null)">
                        <i class="fa fa-plus" [title]="addTitle"></i>
                    </a>                    
                    <a *ngIf="hasExport" class="waves-effect waves-teal btn-flat" (click)="exportClick.emit(null)">
                        <i class="fa fa-download" [title]="exportTitle"></i>
                    </a>
                </div>          
                `
})

export class TileActionsComponent {
    @Output() addClick = new EventEmitter();
    @Output() exportClick = new EventEmitter();

    @Input() hasAdd: boolean = false;
    @Input() hasExport: boolean = false;
    @Input() addTitle: string = "Add";
    @Input() exportTitle: string = "Export";
    
}