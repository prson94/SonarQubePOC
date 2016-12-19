import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionProcessError } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-process-errors',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading">
                    <header>Fusion Processing Error History</header>
                    <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                    <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="errors" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="Error" header="Error" [sortable]="true" [style]="{width:'300px'}">
                            <template let-col let-item="rowData" pTemplate type="body">
                                    <div style="max-height:300px;overflow:auto;" [title]="item.Error">{{item.Error}}</div>
                            </template>
                        </p-column>
                        <p-column field="FusionType" header="Type" [sortable]="true" [style]="{width:'150px'}"></p-column>                        
                        <p-column field="Fusion" header="Configuration" [sortable]="true" [style]="{width:'150px'}"></p-column>                        
                        <p-column field="Date" header="Date" [sortable]="true" [style]="{width:'150px'}">
                            <template let-col let-data="rowData" pTemplate type="body">
                                <span>{{data.Date | date: 'short'}}</span>
                            </template>
                        </p-column>                                                
                    </p-dataTable>      
                </span>
          `,
    providers: [FusionService],
})

export class FusionProcessErrorsComponent extends BaseComponent implements OnInit {
    private errors: FusionProcessError[] = [];
    private selected: FusionProcessError;

    @Input() maxRows: number = 1000;
    @Input() days: number = 0; // 0 = all up to max

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionProcessErrorHistory(this.maxRows,this.days)
            .then(res => {
                this.errors = res;
                this.selected = this.errors.length > 0 ? this.errors[0] : null;
                this.isLoading = false;
            });
    }
};