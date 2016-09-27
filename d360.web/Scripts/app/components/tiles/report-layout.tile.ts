
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import { Report, ReportLayout } from '../../models/report.model';
import { MessagesService, ReportsService  } from '../../services/index';


@Component({
    selector: 'd3s-report-layout-tile',
    providers: [ReportsService],
    template: `
               <header>Dashboard Layout</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="report" *ngIf="!isLoading">
                    <div class="row">
                        <div *ngFor="let cell of layout.cells" [class]="'col s'+ cell.length" >
                            <div *ngFor="let area of cell.areas" class="report-area-design">
                                <span *ngFor="let tile of area.tiles">
                                    <h3>{{tile.Name}}</h3>                                
                                    <div>
                                        <i [class]="'fa fa-4x '+tile.Icon"></i>
                                    </div>
                                </span>
                            </div>
                        </div>
                    </div>
                </div>
                `
})

export class ReportLayoutTile implements OnChanges {
    @Input() report: Report = null;
            
    isLoading: boolean = false;

    layout: ReportLayout;
    
    constructor(private reportsService: ReportsService) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.report != null) this.getLayout();
    }

    private getLayout() {
        this.isLoading = true;
        this.reportsService.getReportLayout(this.report)
            .then(result => {
                console.log(result);
                this.layout = result;
                this.isLoading = false;
                
            });
    }
    
}


