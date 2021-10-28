import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { GridFilterExpression, GridFilterColumn, GridFilterFieldType } from '../../models/grid-definition.model';
import { FilterField, FilterFieldType, FilterExpression } from '../../models/filter-field.model';
import { FormHelpers } from '../../static/form-helpers';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflowmonitor-list-column-filter',
    providers: [],
    styles: [`
        div.filter {
            padding-bottom:5px;
        }    
        div.buttons {
            padding-left: 10px;
            padding-bottom: 5px;
        }    
  `],
    template: ` 
                <form (ngSubmit)="onSubmit()"   (keydown.enter)="$event.preventDefault()" #filterForm="ngForm">
                    <div *ngFor="let filter of internalFilters;let first=first;let last=last;let index=index" class="row filter">
                        <div class="col s2 FieldName" style="padding-left: 0px">Filter:</div>
                        <div class="col s4" style="padding-right: 0px">
                            <select [name]="'FilterField_' + index" required [ngModel]="filter.Field" (ngModelChange)="filter.Field = $event;changeFilterField($event,filter)" style="width:100%;">
                               <option [value]=""></option>
                                <option *ngFor="let p of availableFilters" [ngValue]="p">{{p.Name}}</option>
                            </select>
                        </div>
                        <div [ngSwitch]="filter.Type" class="col s4">
                           <span  [ngSwitch]="filter.Field?.Data?.filtertype">
                                    <span *ngSwitchCase="'list' || 'checkedlist'"   >
                                        <select [name]="'FilterValue_' + index" [ngModel]="filter?.Data?.value" (ngModelChange)="filter.Data.value = $event" required style="width:100%;" placeholder="Choose a field">                                            
                                            <option *ngFor="let p of filter.Field?.Data?.filteritems" [value]="p">{{p}}</option>
                                        </select>
                                    </span>
                                    <p-calendar *ngSwitchCase="'date'" [name]="'FilterValue_' + index" [ngModel]="prepareDateValueForCalendar(filter)" [dateFormat]="getLocaleDateString()" placeholder="{{getLocaleDateString()}}"   [showIcon]="true" (onBlur)="onDateBlur(filter)"   (onSelect)="onDateSelected($event,filter)" ></p-calendar>
                                    <input *ngSwitchDefault [name]="'FilterValue_' + index" type="text" required [ngModel]="filter?.Data?.value" (ngModelChange)="filter.Data.value = $event" placeholder="Enter a value" style="width:100%;"> 
                                </span>   
                        </div>
                        <div class="col s2">
                            <a (click)="addFilter()" class="fa-stack fa-lg overlayed-primary" pTooltip="Add Filter">                                
                                <i class="fa fa-plus fa-stack-1x" style="color:darkgreen;"></i>                                
                            </a> 
                            <a *ngIf="internalFilters.length > 1" (click)="removeFilter(filter)" class="fa-stack fa-lg" pTooltip="Remove Filter" >
                                <i class="fa fa-minus fa-stack-1x" style="color:red;"></i>                                
                            </a>
                        </div>                                                
                    </div>
                    <div class="row">
                     
                        <div class="col s12 buttons">
                            <button pButton *ngIf="internalFilters.length > 0" type="submit" [disabled]="!filterForm.form.valid" style="width: '150px';" label="Filter Results"></button>
                            <button pButton *ngIf="internalFilters.length" type="button" style="width: '150px';" label="Clear all Filters" (click)="resetFilters()"></button>                        
                        </div>
                    </div>
                </form>
                `,
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class WorkflowMonitorListColumnFilterComponent implements OnInit, OnChanges {
    @Input() fields: GridFilterColumn[];
    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();
    connectors: SelectItem[] = [{ label: "And", value: "All" }, { label: "Or", value: "Any" }];
    filterFieldType = FilterFieldType;
    internalFilters: FilterExpression[] = [];
    availableFilters: FilterField[] = [];
    selectedFilter: any;

    constructor(
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef) {
    }


    ngOnInit() {
        if (!this.filters || this.filters.length == 0)
            this.addFilter();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {

        if (changes["fields"] && this.fields != null && this.fields.length > 0) {
            this.availableFilters = [];
            for (let field of this.fields) {
                this.availableFilters.push({
                    Data: field, Name: `${field.text}`, Type: FilterFieldType.Field
                });
            }
            if (this.filters.length > 0) {
                this.internalFilters = this.internalFilters.filter(x => x.Type != FilterFieldType.Field);

                for (let filter of this.filters) {
                    this.internalFilters.push({
                        Type: FilterFieldType.Field,
                        Data: filter,
                        Field: this.availableFilters.filter(x => x.Type == FilterFieldType.Field && x.Data.datafield == filter.field)[0],
                    });
                }
            }
        }
    }

    onSubmit() {
        this.filters = [];
        for (let internalFilter of this.internalFilters) {
            if (internalFilter.Type == FilterFieldType.Field && internalFilter.Data.value) {
                this.filters.push(internalFilter.Data);
            }
        }
        this.filtersChange.emit(this.filters);
    }

    private onDateSelected($event, filter) {
        let d = new Date(Date.parse($event));
        if (d.toString() != "Invalid Date") {
            filter.Data.value = this.getUTCFormattedDateForSearch(d, false, false);
        }
    }

    private onDateBlur(filter) {
        let d = new Date(Date.parse(filter.Data.value));
        if (d.toString() != "Invalid Date")
            filter.Data.value = this.getUTCFormattedDateForSearch(d, true, false);
        else
            filter.Data.value = null;
    }

    private prepareDateValueForCalendar(filter): string {
        let d = new Date(Date.parse(filter.Data.value));
        if (d.toString() != "Invalid Date")
            return this.getUTCFormattedDateForSearch(d, true, true);
        else
            return null;
    }

    private getUTCFormattedDateForSearch(date: Date, isReverse: boolean, isForUI: boolean): string {
        let utcDate: Date = null;
        if (isReverse)
            utcDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
        else
            utcDate = new Date(date.getTime() + date.getTimezoneOffset() * 60000);

        if (isForUI)
            return utcDate.toLocaleDateString()

        return `${utcDate.getMonth() + 1}/${utcDate.getDate()}/${utcDate.getFullYear()} ${utcDate.toTimeString().split(' ')[0]}`;
    }

    public resetFilters() {
        this.internalFilters.splice(0, this.internalFilters.length);
        this.internalFilters.push(new FilterExpression());
        this.filters.splice(0, this.filters.length);
        this.filtersChange.emit(this.filters);
    }

    private changeFilterField(target, filter) {
        if (target.Type == FilterFieldType.Field) {
            filter.Data = new GridFilterExpression();
            filter.Data.field = target.Data.datafield;
            filter.Type = FilterFieldType.Field;

            if (target.Data.columntype == "dropdownlist" || target.Data.columntype == "numberinput")
                filter.Data.condition = "EQUAL";
            else
                filter.Data.condition = "CONTAINS";

            //determine the field type
            if (target.Data.hiddenfield)
                filter.Data.fieldtype = GridFilterFieldType.Hidden;
            else
                filter.Data.fieldtype = GridFilterFieldType.Normal;
        }
    }

    private addFilter() {
        this.internalFilters.push(new FilterExpression());
        setTimeout(() => {
            this.ref.markForCheck();
        }, 50);
    }


    private removeFilter(filter: FilterExpression) {
        let index = this.internalFilters.indexOf(filter);
        this.internalFilters.splice(index, 1);
        setTimeout(() => {
            this.ref.markForCheck();
        }, 50);
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }
}

