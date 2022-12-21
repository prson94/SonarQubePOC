import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input, OnInit,
    Output
} from '@angular/core';
import { map } from 'rxjs/operators';
import { State } from '../../models/asset.model';
import { GridFilterColumn, GridFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { WorkflowTypeModel } from '../../models/workflow.model';
import { CompanySettingsService } from '../../services/settings.service';
import { WorkflowService } from "../../services/workflow.service";
import { WorkflowMonitorService } from '../../services/workflowmonitor.service';
import { StringHelpers } from '../../static/string-helpers';
import { BaseComponent } from "../shared/base.component";

@Component({
    selector: 'd3s-workflowmonitor-list-filter',
    templateUrl: 'workflowmonitor-list-filter.component.html',
    providers: [WorkflowService, WorkflowMonitorService],
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class WorkflowMonitorListFilterComponent extends BaseComponent implements OnInit {
    @Input() selectAll: boolean = false;
    @Input() selection: string[];
    @Output() filterChange = new EventEmitter();
    @Output() exportToExcel = new EventEmitter();
    @Input() columnFilters: GridFilterExpression[] = [];
    @Output() columnFiltersChange = new EventEmitter();
    @Input() workflowTypeFilters: GridFilterExpression;
    @Output() workflowTypeFiltersChange = new EventEmitter();
    @Input() showExport: boolean = false;
    @Input() usePredefinedFilters: boolean = false;
    @Output() exportClick = new EventEmitter();
    @Input() isExportDisabled: boolean = false;
    @Input() exportDisabledMessage: string = $localize`Export Disabled`;

    items: WorkflowTypeModel[];
    filtercolumns: GridFilterColumn[] = [];

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


    columFilterChanged(e: GridFilterExpression[]) {
        this.columnFilters = e;
        this.columnFiltersChange.emit(this.columnFilters);
        this.filterChange.emit();
    }

    change(e: string[]) {
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
