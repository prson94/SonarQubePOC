import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges
} from '@angular/core';
import { BaseComponent } from "../shared/base.component";
import { WorkflowService } from "../../services/workflow.service";
import { GridFilterColumn, GridFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { WorkflowMonitorService } from '../../services/workflowmonitor.service';
import { StringHelpers } from '../../static/string-helpers';
import { State } from '../../models/asset.model';
import { map } from 'rxjs/operators';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflowmonitor-list-filter',
    templateUrl: 'workflowmonitor-list-filter.component.html',
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
                map((r) => {
                    this.items = r;

                    this.items.forEach((i) => {
                        i.label = i.State === State.InActive ? i.Name + " ( " + $localize`Inactive` + " )" : i.Name;
                        i.value = i.ID.toString();
                    });

                    this.selection = [];
                    if (this.usePredefinedFilters) {
                        this.items.forEach((i) => this.selection.push(i.value));
                        this.change(this.selection);
                    }
                    else if (this.workflowTypeFilters && !StringHelpers.isNullOrEmpty(this.workflowTypeFilters.value)) {
                        this.workflowTypeFilters.value.split(",").forEach((i) => {
                            if (!(StringHelpers.isNullOrEmpty(i)))
                                {this.selection.push(i);}
                        });
                    } else if (this.selectAll) {
                        this.items.forEach((i) => this.selection.push(i.value));
                        this.change(this.selection);
                    }


                    this.isLoading = false;
                }),
                map(() => this.wfMonitorService.getWorkFlowMonitorFilterColumnDefinition()
                    .subscribe((x) => {
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
            {return;}
        const data = new GridFilterExpression();
        data.field = "WorkflowId";
        data.condition = "IN";
        data.fieldtype = GridFilterFieldType.Normal;

        let typeList = "";
        e.forEach((s) => typeList += s.toString() + ',');
        data.value = typeList;
        this.workflowTypeFilters = typeList !== "" ? data : null;
        this.workflowTypeFiltersChange.emit(this.workflowTypeFilters);
        this.filterChange.emit();
    }

}