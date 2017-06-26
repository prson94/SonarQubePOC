import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionExecutionError } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-execution-errors',
    template: `        
                    <header>Execution History - Error Details<d3s-tile-actions [hasExport]="true" (exportClick)="export()"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                        <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="errors" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" >
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="Date" header="Date" [sortable]="true" [style]="{width:'100px'}">
                                <ng-template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.Date | date: 'short'}}</span>
                                </ng-template>
                            </p-column>
                            <p-column field="Error" header="Error" [sortable]="true" [style]="{width:'175px'}"></p-column>                        
                        </p-dataTable>      
                    </span>                
          `,
    providers: [FusionService],
})

export class FusionExecutionErrorsComponent extends BaseComponent implements OnInit {    
    @Input() executionId: number;

    private errors: FusionExecutionError[] = [];
    private selected: FusionExecutionError;
    
    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {       
        this.isLoading = true;
        this.fusionService.getFusionExecutionErrors(this.executionId)
            .then(res => {
                this.errors = res;
                this.selected = this.errors.length > 0 ? this.errors[0] : null;
                this.isLoading = false;
            });
    }  

    private export() {
        this.fusionService.getFusionExecutionErrorsExport(this.executionId)
    }
};