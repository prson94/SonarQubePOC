
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionExecutionResult } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-execution-results',
    template: `     <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                        <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="results" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" >                            
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="FusionAttributeType" header="Type" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="FusionAttribute" header="Attribute" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="Action" header="Action" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="FieldName" header="Field" [sortable]="true" [style]="{width:'125px'}"></p-column>                        
                            <p-column field="OldValue" header="Old Value" [sortable]="true" [style]="{width:'175px'}"></p-column>                        
                            <p-column field="NewValue" header="New Value" [sortable]="true" [style]="{width:'175px'}"></p-column>
                        </p-dataTable>      
                    </span>
          `,
    providers: [FusionService],
})

export class FusionExecutionResultsComponent extends BaseComponent implements OnInit {
    @Input() executionId: number;

    private results: FusionExecutionResult[] = [];
    private selected: FusionExecutionResult;

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionExecutionResults(this.executionId)
            .then(res => {
                this.results = res;
                this.selected = this.results.length > 0 ? this.results[0] : null;
                this.isLoading = false;
            });
    }
};