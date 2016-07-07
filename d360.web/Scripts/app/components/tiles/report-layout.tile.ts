///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { Report, ReportLayoutRow } from '../../models/report.model';
import { MessagesService, ReportsService  } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import {DeleteForm} from '../forms/delete.form';


@Component({
    selector: 'd3s-report-layout-tile',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm],
    providers: [ReportsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Dashboard Layout</header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="report" *ngFor="let row of layout">
                    <div class="row" *ngFor="let cell of row.cells">
                        <div *ngFor="let area of cell.areas" [class]="'col s'+ cell.length" >
                            <div *ngFor="let tile of area.tiles" class="report-area-design">
                                <h3>{{tile.Name}}</h3>
                                <div>
                                    <i [class]="'fa fa-4x '+tile.Icon"></i>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                `
})

export class ReportLayoutTile implements OnChanges {
    @Input() report: Report = null;
            
    isLoading: boolean = false;

    layout: ReportLayoutRow[];
    
    constructor(private reportsService: ReportsService) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.report != null) this.getLayout();
    }

    private getLayout() {
        this.reportsService.getReportLayout(this.report)
            .then(result => {
                this.layout = result;
                this.isLoading = false;
                console.log(this.layout);
            });
    }
    
}


