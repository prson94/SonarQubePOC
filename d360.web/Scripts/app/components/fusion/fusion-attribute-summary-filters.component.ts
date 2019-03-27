import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeService } from '../../services/fusion-attribute.service';
import { FusionAttributeValueDetails, FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-attribute-summary-filters',
    template: ` <form (ngSubmit)="filterResults()" #filterForm="ngForm">
                <div class="row advSearchRow" *ngFor="let filter of internalFilters;let first=first; let last = last;let i = index">
                    <div class="col s1 center-align">Field:</div>
                    <div class="col s3">                        
                        <select [name]="'field'+i" required [ngModel]="filter.dataField" (ngModelChange)="setFieldType(filter,$event);filter.dataField = $event;" style="width:100%;" #field="ngModel">
                            <option></option>
                            <option *ngFor="let p of filterColumns" [value]="p.datafield">{{p.text}}</option>
                        </select>
                        <div [hidden]="field.valid || field.pristine">A field is required</div>                                                                        
                    </div>
                    <div class="col s3" [ngSwitch]="filter.columnType">
                        <select required [name]="'value'+i" [(ngModel)]="filter.value" style="width:100%;" *ngSwitchCase="'dropdownlist'">
                            <option></option>
                            <option *ngFor="let p of fieldOptions(filter.dataField)" [value]="p">{{p}}</option>
                        </select>
                        <input required maxlength="250" placeholder="Filter value..." [name]="'value'+i" type="text" pInputText *ngSwitchDefault [(ngModel)]="filter.value" style="width: 100%;" />                        
                    </div>
                    <div class="col s1" *ngIf="last">
                        <button pButton type="button" (click)="addFilter()" label="+" ></button>
                    </div>
                    <div class="col s1" *ngIf="!last">
                        <button pButton type="button" (click)="removeFilter(i)" label="-" ></button>
                    </div>
                    <div class="col s3" *ngIf="last">
                        <button *ngIf="!isFiltering" pButton type="button" (click)="removeAllFilters()" label="Clear All"></button>
                        <button *ngIf="!isFiltering" pButton type="submit" label="Filter"></button>                        
                        <i *ngIf="isFiltering" class="fa fa-spinner fa-spin fa-2x"></i>                        
                    </div>  
                    <div *ngIf="first && hasExport"   [ngClass]="first==last ? 'col s1' : 'col s1 offset-s3'">
                          <d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="exportClick.emit()"></d3s-tile-actions>
                    </div>

                </div>
                </form>
                `,
    providers: [FusionAttributeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class FusionAttributeSummaryFiltersComponent extends BaseComponent implements OnChanges {
    @Input() filters: FusionAttributeFilter[] = [];
    @Output() filtersChange = new EventEmitter();

    @Input() filterColumns: GridFilterColumn[];
    @Input() isFiltering: boolean = false;
    @Input() hasExport: boolean = false;
    @Output() exportClick = new EventEmitter();
    private internalFilters: FusionAttributeFilter[] = [];


    constructor(private fusionAttributeService: FusionAttributeService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['filterColumns'] && this.filterColumns.length > 0) {
            if (this.filters.length == 0 && this.internalFilters.length == 0)
                this.internalFilters.push(new FusionAttributeFilter());
            else
                this.internalFilters = _.cloneDeep(this.filters);

        }
        else if (changes['filters'] && this.filters.length > 0) {            
            this.internalFilters = _.cloneDeep(this.filters);
        }
    }

    private addFilter() {
        this.internalFilters.push(new FusionAttributeFilter());            
    }

    private filterResults() {        
        this.filters = _.cloneDeep(this.internalFilters);
        this.filtersChange.emit(this.filters);
    }

    private removeFilter(index) {
        if (index < 0 || index > this.filters.length) {
            console.log("ERROR : INVALID INDEX SPECIFIED TO REMOVE FILTER FOR.");

            return;
        }
        this.internalFilters.splice(index, 1);
    }

    private removeAllFilters() {
        this.filters.splice(0, this.filters.length);
        this.internalFilters.splice(0, this.internalFilters.length);
        this.filtersChange.emit(this.filters);

        this.filters.push(new FusionAttributeFilter());            
        this.internalFilters.push(new FusionAttributeFilter());            
    }

    private fieldOptions(dataField: string): string[] {
        let results = this.filterColumns.filter(x => x.datafield == dataField);

        if (results && results.length > 0) {
            return results[0].filteritems;
        }
        return [];
    }

    private setFieldType(filter: FusionAttributeFilter, dataField: string): void {       
        let results = this.filterColumns.filter(x => x.datafield == dataField);

        if (results && results.length > 0) {
            filter.columnType = results[0].columntype;
            
            if (filter.columnType == 'dropdownlist') filter.condition = 'EQUALS';
            else filter.condition = 'CONTAINS';
        }
        else
            filter.columnType = "";        
    }    
};