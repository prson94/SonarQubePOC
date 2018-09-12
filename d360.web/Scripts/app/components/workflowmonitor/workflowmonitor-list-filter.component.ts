import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from "../shared/base.component";
import { WorkflowService } from "../../services/workflow.service";
import { GridFilterColumn, GridFilterExpression, GridAttributeFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { WorkflowMonitorService } from '../../services/workflowmonitor.service';
import { FilterFieldType } from '../../models/filter-field.model';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-workflowmonitor-top-level-filter',
    template: ` 
                    <div class="row">
                        <div class="col s3 FieldName">Workflow Types</div>
                        <div class="col s9">
                           <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
                            <div *ngIf="!isLoading">
                                <p-multiSelect [options]="items" [style]="{'width':'100%'}" [ngModel]="selection" (ngModelChange)="change($event)"></p-multiSelect>
                            </div>        
                        </div>
                    </div>   
                    <div class="row">
                        <d3s-workflowmonitor-list-column-filter (exportToExcel)="exportToExcel.emit()" [fields]="filtercolumns" (filtersChange)="columFilterChanged($event)" ></d3s-workflowmonitor-list-column-filter>
                    </div>
                `,
    providers: [WorkflowService, WorkflowMonitorService]
})


export class WorkflowMonitorListFilterComponent extends BaseComponent  implements OnInit {
    @Input() selectAll: boolean = false;
    @Input() selection: any[];
    @Output() selectionChange = new EventEmitter();
    @Output() filterChange = new EventEmitter();
    items: any[];
    @Output() exportToExcel = new EventEmitter();
    filtercolumns: GridFilterColumn[] = [];
    columfilter: GridFilterExpression[] = [];
    itemfilter: GridFilterExpression;

    constructor(protected workflowService: WorkflowService, protected wfMonitorService:WorkflowMonitorService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getTypes()
            .then(r => {
                this.items = r;

                this.items.forEach(i => {
                    i.label = i.Name;
                    i.value = i.ID.toString();
                });

                this.selection = [];
                if (this.selectAll)
                    this.items.forEach(i => this.selection.push(i.value));

                this.change(this.selection);
                this.isLoading = false;
            }).then(() => this.wfMonitorService.getWorkFlowMonitorFilterColumnDefinition())
            .then(x => {
                this.filtercolumns = x;
                this.isLoading = false;
            })
    }

    onFilterChange() {
        //debugger;
        let clone = _.clone(this.columfilter);
        if (this.itemfilter)
            clone.push(this.itemfilter)
        this.filterChange.emit(clone);
    }
    columFilterChanged(e) {
        this.columfilter = e;
        this.onFilterChange();
    }

    change(e: any) {
        let data = new GridFilterExpression();
        data.field = "WorkflowId";
        data.condition = "IN";
        data.fieldtype = GridFilterFieldType.Normal;

        let typeList = "";
        e.forEach(s => typeList += s.toString() + ',');

        
        data.value = typeList;
        if(typeList !="")
            this.itemfilter = data;
        else 
            this.itemfilter = null;

        this.onFilterChange();
            
        }
 
};