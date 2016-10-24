
import { Input, Component, EventEmitter, Output, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionAttributeService } from '../../services/index';
import { FusionAttributeValueDetails, FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-attribute-summary-filters',
    template: ` <form (ngSubmit)="filterResults()" #filterForm="ngForm">
                <div class="row advSearchRow" *ngFor="let filter of internalFilters;let last = last;let i = index">
                    <div class="col s1 center-align">Field:</div>
                    <div class="col s3">                        
                        <select [name]="'field'+i" required [(ngModel)]="filter.dataField" style="width:100%;" #field="ngModel">
                            <option *ngFor="let p of filterColumns" [value]="p.datafield">{{p.text}}</option>
                        </select>
                        <div [hidden]="field.valid || field.pristine">A field is required</div>                                                                        
                    </div>
                    <div class="col s3" [ngSwitch]="typeOfField(filter.dataField)">
                        <select required [name]="'value'+i" [(ngModel)]="filter.value" style="width:100%;" *ngSwitchCase="'dropdownlist'">
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
                    <div class="col s1 offset-s1" *ngIf="last">
                        <button pButton type="button" (click)="removeAllFilters()" label="Clear All" style="width: 100px;"></button>
                    </div>
                    <div class="col s1" *ngIf="last"><button pButton type="submit" label="Filter" style="width: 100px; " ></button>
                    </div>
                </div>
                </form>
                `,
    providers: [FusionAttributeService],
})

export class FusionAttributeSummaryFiltersComponent extends BaseComponent implements OnChanges {
    @Input() filters: FusionAttributeFilter[] = [];
    @Output() filtersChange = new EventEmitter();

    @Input() filterColumns: GridFilterColumn[];

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
            console.log(this.filters);
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

    private typeOfField(dataField: string): string {
        let results = this.filterColumns.filter(x => x.datafield == dataField);

        if (results && results.length > 0) {
            return results[0].columntype;
        }
        return "";
    }
};