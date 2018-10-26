import { Component, Input, OnChanges, SimpleChanges, OnInit } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { WorkflowStepFieldChangeDetail } from "../../models/workflow.model";


@Component({
    selector: 'd3s-workflow-monitor-step-field-change-details',
    template:
        `
                <div class="row" >                    
                    <div class="col s12">                
                    <p-dataTable #dt [value]="fieldChanges" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3">                                                                        
                    <p-footer *ngIf="dt.totalRecords">
                        <d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info>
                    </p-footer>
                        <p-column field="FieldName" header="Name" sortable="true" ></p-column>  
                        <p-column field="Value" header="Value" sortable="true" ></p-column>  
                        <p-column field="FormValue" header="Form" sortable="true" ></p-column>  
                        <p-column field="AppendValue" header="Appended" sortable="true" ></p-column> 
                         <p-column field="ClearValue" header="Cleared" sortable="true" ></p-column>  
                      </p-dataTable>                          
                    </div>
                </div>               
`,

})
export class WorkflowMonitorStepFieldChangeDetailsComponent extends BaseComponent implements OnInit, OnChanges {

    
    @Input() fieldChanges: any;

    ngOnInit(): void {
        debugger;
        console.log(this.fieldChanges);
    }

    ngOnChanges(changes: SimpleChanges): void {
        console.log(changes['fieldChanges'])
    }
}