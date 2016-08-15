///<reference path="../../es6-shim.d.ts"/>
import {Component, EventEmitter, Output, Input} from '@angular/core';
import {DataTable} from 'primeng/primeng';

@Component({
    selector: 'd3s-tile-actions',
    styles: [`
    .btn-flat {
        padding:0;
        margin-left:10px;
    }
    .spacer {
        
    }

    .disabled {
        color:#d3d5d8;
        pointer-events: none;
        cursor: default;
    }
  `],
    template: `
                <div id="FieldsTile_tools" class="TileTools">
                    <a *ngIf="hasAdd" class="waves-effect waves-teal btn-flat" (click)="addClick.emit(null)" [ngClass]="{'disabled':!addEnabled}">
                        <i class="fa fa-plus" [title]="addTitle"></i>
                    </a>                    
                    <a *ngIf="hasExport" class="waves-effect waves-teal btn-flat" (click)="exportClick.emit(null)" [ngClass]="{'disabled':!exportEnabled}">
                        <i class="fa fa-download" [title]="exportTitle"></i>
                    </a>
                    <a *ngIf="hasEdit" class="waves-effect waves-teal btn-flat" (click)="editClick.emit(null)" [ngClass]="{'disabled':!editEnabled}">
                        <i class="fa fa-pencil" [title]="editTitle"></i>
                    </a>
                    <a *ngIf="grid" class="waves-effect waves-teal btn-flat" (click)="doGridExport()">
                        <i class="fa fa-download" [title]="exportTitle"></i>
                    </a>
                </div>          
                `
})

export class TileActionsComponent {
    @Output() addClick = new EventEmitter();
    @Output() exportClick = new EventEmitter();
    @Output() editClick = new EventEmitter();

    @Input() hasAdd: boolean = false;
    @Input() hasExport: boolean = false;
    @Input() hasEdit: boolean = false;
    @Input() addTitle: string = "Add";
    @Input() exportTitle: string = "Export";
    @Input() editTitle: string = "Edit";
    @Input() grid: DataTable;

    @Input() exportEnabled: boolean = true;
    @Input() addEnabled: boolean = true;
    @Input() editEnabled: boolean = true;

    private doGridExport() {
        this.grid.exportCSV();
    }    
}