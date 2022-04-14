import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from "../shared/base.component";
import { WorkflowService } from "../../services/workflow.service";
import { GridFilterColumn, GridFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { WorkflowMonitorService } from '../../services/workflowmonitor.service';
import * as _ from 'lodash';
import { StringHelpers } from '../../static/string-helpers';
import { State } from '../../models/asset.model';
import { map } from 'rxjs/operators';
import { CompanySettingsService } from '../../services/settings.service';



@Component({
    selector: 'd3s-workflowmonitor-list-filter',
    template: ` 

                <ng-container *ngIf="!usePredefinedFilters; else exportOnly">
                    <div class="row">
                        <div class="col s2 FieldName" style="padding-left: 0px" i18n>Workflow Types</div>
                        <div class="col s10" style="padding-right: 0px">
                           <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
                            <div *ngIf="!isLoading">
                                <table style="table-layout: fixed">
                                    <tbody>
                                        <tr>
                                            <td>
                                                <p-multiSelect [options]="items" [style]="{'width':'98%'}" [ngModel]="selection" (ngModelChange)="change($event)" selectedItemsLabel="{0} items selected"></p-multiSelect>
                                            </td>
                                            <td [ngClass]="{'actions-disabled':isExportDisabled}" *ngIf="showExport" style="width:32px">
                                                <a class="Action" style="font-size:1.1em" (click)="isExportDisabled ? return : exportClick.emit()" [pTooltip]="exportTooltip"><i class="fa fa-download fa-fw"></i></a>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>        
                        </div>
                    </div>
                    <div class="row">
                        <d3s-workflowmonitor-list-column-filter (exportToExcel)="exportToExcel.emit()" [fields]="filtercolumns" [(filters)]="columnFilters" (filtersChange)="columFilterChanged($event)" ></d3s-workflowmonitor-list-column-filter>
                    </div>
                </ng-container>                  
                <ng-template #exportOnly>
                    <div class="row">
                        <div class="col s12" style="padding-right: 0px">
                            <div [ngClass]="{'actions-disabled':isExportDisabled}" *ngIf="showExport" style="width: 32px; float: right">
                                <a class="Action" style="font-size:1.1em" (click)="isExportDisabled ? return : exportClick.emit()" [pTooltip]="exportTooltip"><i class="fa fa-download fa-fw"></i></a>
                            </div>
                        </div>
                    </div>
                </ng-template>

                `,
    providers: [WorkflowService, WorkflowMonitorService],
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class WorkflowMonitorListFilterComponent extends BaseComponent implements OnInit, OnChanges {

    @Input() selectAll: boolean = false;
    @Input() selection: any[];
    @Output() filterChange = new EventEmitter();
    items: any[];
    @Output() exportToExcel = new EventEmitter();
    filtercolumns: GridFilterColumn[] = [];
    @Input() columnFilters: GridFilterExpression[] = [];
    @Output() columnFiltersChange = new EventEmitter();
    @Input() workflowTypeFilters: GridFilterExpression;
    @Output() workflowTypeFiltersChange = new EventEmitter();
    @Input() showExport: boolean = false;
    @Input() usePredefinedFilters: boolean = false;
    @Output() exportClick = new EventEmitter();
    @Input() isExportDisabled: boolean = false;
    @Input() exportDisabledMessage: string = $localize`Export Disabled`;

    get exportTooltip(): string {
        return this.isExportDisabled ? this.exportDisabledMessage : $localize`Export to Excel`;
    }

    constructor(protected workflowService: WorkflowService,
        protected ref: ChangeDetectorRef,
        protected settingsService: CompanySettingsService,
        protected wfMonitorService: WorkflowMonitorService) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges): void {

    }
    private load() {
        this.isLoading = true;
        this.workflowService.getTypes()
            .pipe(
                map(r => {
                    this.items = r;

                    this.items.forEach(i => {
                        i.label = i.State == State.InActive ? i.Name + " ( " + $localize`Inactive` + " )" : i.Name;
                        i.value = i.ID.toString();
                    });

                    this.selection = [];
                    if (this.usePredefinedFilters) {
                        this.items.forEach(i => this.selection.push(i.value));
                        this.change(this.selection);
                    }
                    else if (this.workflowTypeFilters && !StringHelpers.isNullOrEmpty(this.workflowTypeFilters.value)) {
                        this.workflowTypeFilters.value.split(",").forEach(i => {
                            if (!(StringHelpers.isNullOrEmpty(i)))
                                this.selection.push(i);
                        });
                    } else if (this.selectAll) {
                        this.items.forEach(i => this.selection.push(i.value));
                        this.change(this.selection);
                    }


                    this.isLoading = false;
                }),
                map(() => this.wfMonitorService.getWorkFlowMonitorFilterColumnDefinition()
                    .subscribe(x => {
                        this.filtercolumns = x;
                        this.isLoading = false;
                        this.ref.markForCheck();
                    }))
            ).subscribe();
    }


    columFilterChanged(e) {
        this.columnFilters = e;
        this.columnFiltersChange.emit(this.columnFilters);
        this.filterChange.emit();
    }

    change(e: any) {
        if (this.usePredefinedFilters)
            return;
        let data = new GridFilterExpression();
        data.field = "WorkflowId";
        data.condition = "IN";
        data.fieldtype = GridFilterFieldType.Normal;

        let typeList = "";
        e.forEach(s => typeList += s.toString() + ',');
        data.value = typeList;
        this.workflowTypeFilters = typeList != "" ? data : null;
        this.workflowTypeFiltersChange.emit(this.workflowTypeFilters);
        this.filterChange.emit();
    }

}