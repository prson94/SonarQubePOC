import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from "../shared/base.component";
import { WorkflowService } from "../../services/workflow.service";
import { GridFilterColumn, GridFilterExpression, GridAttributeFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { WorkflowMonitorService } from '../../services/workflowmonitor.service';
import { FilterFieldType } from '../../models/filter-field.model';
import * as _ from 'lodash';
import { StringHelpers } from '../../static/string-helpers';


@Component({
    selector: 'd3s-workflowmonitor-list-filter',
    template: ` 
                    <div class="row">
                        <div class="col s2 FieldName" style="padding-left: 0px">Workflow Types</div>
                        <div class="col s10" style="padding-right: 0px">
                           <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
                            <div *ngIf="!isLoading">
                                <p-multiSelect [options]="items" [style]="{'width':'100%'}" [ngModel]="selection" (ngModelChange)="change($event)"></p-multiSelect>
                            </div>        
                        </div>
                    </div>   
                    <div class="row">
                        <d3s-workflowmonitor-list-column-filter (exportToExcel)="exportToExcel.emit()" [fields]="filtercolumns" [(filters)]="columnFilters" (filtersChange)="columFilterChanged($event)" ></d3s-workflowmonitor-list-column-filter>
                    </div>
                `,
    providers: [WorkflowService, WorkflowMonitorService]
})


export class WorkflowMonitorListFilterComponent extends BaseComponent  implements OnInit,OnChanges {
    
    @Input() selectAll: boolean = false;
    @Input() selection: any[];
    @Output() selectionChange = new EventEmitter();
    @Output() filterChange = new EventEmitter();
    items: any[];
    @Output() exportToExcel = new EventEmitter();
    filtercolumns: GridFilterColumn[] = [];
    //columfilter: GridFilterExpression[] = [];
    //itemfilter: GridFilterExpression;
    @Input()  columnFilters: GridFilterExpression[] = [];
    @Output() columnFiltersChange = new EventEmitter();
    @Input() workflowTypeFilters: GridFilterExpression;
    @Output() workflowTypeFiltersChange = new EventEmitter();

    constructor(protected workflowService: WorkflowService, protected wfMonitorService:WorkflowMonitorService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges): void {
        debugger;
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
                if (this.workflowTypeFilters && !StringHelpers.isNullOrEmpty(this.workflowTypeFilters.value)) {
                    this.workflowTypeFilters.value.split(",").forEach(i => this.selection.push(i));
                    //this.change(this.selection);
                } else if (this.selectAll) {
                    this.items.forEach(i => this.selection.push(i.value));
                    this.change(this.selection);
                }

                
                this.isLoading = false;
            }).then(() => this.wfMonitorService.getWorkFlowMonitorFilterColumnDefinition())
            .then(x => {
                this.filtercolumns = x;
                this.isLoading = false;
            })
    }


    columFilterChanged(e) {
        //this.columfilter = e;
        this.columnFilters = e;
       // this.onFilterChange();
        this.columnFiltersChange.emit(this.columnFilters);
        this.filterChange.emit();
    }

    change(e: any) {
        debugger
        let data = new GridFilterExpression();
        data.field = "WorkflowId";
        data.condition = "IN";
        data.fieldtype = GridFilterFieldType.Normal;

        let typeList = "";
        e.forEach(s => typeList += s.toString() + ',');

        
        data.value = typeList;
        if (typeList != "") {
            //this.itemfilter = data;
            this.workflowTypeFilters = data;
        }
        else {
            //this.itemfilter = null;
            this.workflowTypeFilters = null;
        }
        this.workflowTypeFiltersChange.emit(this.workflowTypeFilters);
        this.filterChange.emit();
       // this.onFilterChange();
            
        }
 
};