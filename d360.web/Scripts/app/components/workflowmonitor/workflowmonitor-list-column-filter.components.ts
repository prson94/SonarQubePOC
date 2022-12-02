import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChange
} from '@angular/core';
import { SelectItem } from 'primeng/api';
import { GridFilterColumn, GridFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { FilterExpression, FilterField, FilterFieldType } from '../../models/filter-field.model';
import { FormHelpers } from '../../static/form-helpers';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflowmonitor-list-column-filter',
    providers: [],
    styleUrls: ['workflowmonitor-list-column-filter.components.less'],
    templateUrl: 'workflowmonitor-list-column-filter.components.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class WorkflowMonitorListColumnFilterComponent implements OnInit, OnChanges {
    @Input() fields: GridFilterColumn[];
    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();
    connectors: SelectItem[] = [{ label: $localize`And`, value: "All" }, { label: $localize`Or`, value: "Any" }];
    filterFieldType = FilterFieldType;
    internalFilters: FilterExpression[] = [];
    availableFilters: FilterField[] = [];
    selectedFilter: any;

    constructor(
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef) {
    }


    ngOnInit() {
        if (!this.filters || this.filters.length === 0)
            {this.addFilter();}
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {

        if (changes["fields"] && this.fields != null && this.fields.length > 0) {
            this.availableFilters = [];
            for (const field of this.fields) {
                this.availableFilters.push({
                    Data: field, Name: `${field.text}`, Type: FilterFieldType.Field
                });
            }
            if (this.filters.length > 0) {
                this.internalFilters = this.internalFilters.filter((x) => x.Type !== FilterFieldType.Field);

                for (const filter of this.filters) {
                    this.internalFilters.push({
                        Type: FilterFieldType.Field,
                        Data: filter,
                        Field: this.availableFilters.filter((x) => x.Type === FilterFieldType.Field && x.Data.datafield === filter.field)[0],
                    });
                }
            }
        }
    }

    onSubmit() {
        this.filters = [];
        for (const internalFilter of this.internalFilters) {
            if (internalFilter.Type === FilterFieldType.Field && internalFilter.Data.value) {
                this.filters.push(internalFilter.Data);
            }
        }
        this.filtersChange.emit(this.filters);
    }

    private onDateSelected($event, filter) {
        const d = new Date(Date.parse($event));
        if (d.toString() !== "Invalid Date") {
            filter.Data.value = this.getUTCFormattedDateForSearch(d, false, false);
        }
    }

    private onDateBlur(filter) {
        const d = new Date(Date.parse(filter.Data.value));
        if (d.toString() !== "Invalid Date")
            {filter.Data.value = this.getUTCFormattedDateForSearch(d, true, false);}
        else
            {filter.Data.value = null;}
    }

    private prepareDateValueForCalendar(filter): string {
        const d = new Date(Date.parse(filter.Data.value));
        if (d.toString() !== "Invalid Date")
            {return this.getUTCFormattedDateForSearch(d, true, true);}
        else
            {return null;}
    }

    private getUTCFormattedDateForSearch(date: Date, isReverse: boolean, isForUI: boolean): string {
        let utcDate: Date = null;
        if (isReverse)
            {utcDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);}
        else
            {utcDate = new Date(date.getTime() + date.getTimezoneOffset() * 60000);}

        if (isForUI)
            {return utcDate.toLocaleDateString();}

        return `${utcDate.getMonth() + 1}/${utcDate.getDate()}/${utcDate.getFullYear()} ${utcDate.toTimeString().split(' ')[0]}`;
    }

    public resetFilters() {
        this.internalFilters.splice(0, this.internalFilters.length);
        this.internalFilters.push(new FilterExpression());
        this.filters.splice(0, this.filters.length);
        this.filtersChange.emit(this.filters);
    }

    private changeFilterField(target, filter) {
        if (target.Type === FilterFieldType.Field) {
            filter.Data = new GridFilterExpression();
            filter.Data.field = target.Data.datafield;
            filter.Type = FilterFieldType.Field;

            if (target.Data.columntype === "dropdownlist" || target.Data.columntype === "numberinput")
                {filter.Data.condition = "EQUAL";}
            else
                {filter.Data.condition = "CONTAINS";}

            //determine the field type
            if (target.Data.hiddenfield)
                {filter.Data.fieldtype = GridFilterFieldType.Hidden;}
            else
                {filter.Data.fieldtype = GridFilterFieldType.Normal;}
        }
    }

    private addFilter() {
        this.internalFilters.push(new FilterExpression());
        setTimeout(() => {
            this.ref.markForCheck();
        }, 50);
    }


    private removeFilter(filter: FilterExpression) {
        const index = this.internalFilters.indexOf(filter);
        this.internalFilters.splice(index, 1);
        setTimeout(() => {
            this.ref.markForCheck();
        }, 50);
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }
}

