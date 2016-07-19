///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, OnChanges, SimpleChange } from '@angular/core';
import { NgForm, REACTIVE_FORM_DIRECTIVES } from '@angular/forms';
import { Button } from 'primeng/primeng';
import { ArtifactService } from '../../services/index';
import { Artifact } from '../../models/artifacts.model';
import { GridFilterExpression, GridFilterColumn } from '../../models/grid-definition.model';


@Component({
    selector: 'd3s-artifact-column-filter',
    directives: [Button],
    styles: [`
        div.filter {
            padding-bottom:5px;
        }    
        div.buttons {
            padding-left: 10px;
        }    
  `],
    template: ` 
                <form (ngSubmit)="onSubmit()" #filterForm="ngForm">
                    <div *ngFor="let filter of filters;let first=first;let last=last;let index=index" class="row filter">
                        <div class="col s1 center-align">Field:</div>
                        <div class="col s4"><select [name]="'FilterField_' + index" required [(ngModel)]="filter.field" (change)="changeFilterField($event.target,filter)" style="width:100%;">                                            
                                                <option *ngFor="let p of fields" [value]="p.datafield">{{p.text}}</option></select>
                        </div>
                        <div class="col s4" [ngSwitch]="selectedFieldType(filter.field)">
                            <span *ngSwitchCase="'dropdownlist'">
                                <select [name]="'FilterValue_' + index" [(ngModel)]="filter.value" required style="width:100%;" placeholder="Choose a field">                                            
                                      <option *ngFor="let p of fieldFilters(filter.field)" [value]="p">{{p}}</option></select>
                            </span>
                            <input *ngSwitchDefault [name]="'FilterValue_' + index" type="text" required [(ngModel)]="filter.value" placeholder="Enter a value" style="width:100%;"> 
                        </div>
                        <div class="col s3">
                            <span (click)="addFilter()"><i *ngIf="last" class="fa fa-plus fa-2x" aria-hidden="true"></i></span> <span *ngIf="filters.length > 1" (click)="removeFilter(filter)"><i class="fa fa-minus fa-2x" aria-hidden="true"></i></span>
                        </div>                        
                    </div>
                    <div class="buttons">
                        <button pButton *ngIf="filters.length > 0" type="submit" [disabled]="!filterForm.form.valid" style="width: '150px';" label="Filter"></button>
                        <button pButton *ngIf="filters.length" type="button" style="width: '150px';" label="Clear" (click)="clearFilter()"></button>
                        <button pButton *ngIf="!filters.length" type="button" style="width: '150px';" label="Add Filter" (click)="addFilter()"></button>
                    </div>
                </form>
                `    
})

export class ArtifactColumnFilterComponent implements OnInit, OnDestroy, OnChanges {
    @Input() fields: GridFilterColumn[];
    @Output() filterChanged = new EventEmitter();

    filters: GridFilterExpression[] = [];

    constructor() {        
        
    }

    ngOnInit() {
        
    }

    ngOnDestroy() {
        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.fields != null && this.fields.length > 0) {
                        
        }
    }

    private onSubmit() {
        this.filterChanged.emit({ filter: this.filters });
    }

    private clearFilter() {
        this.filters.splice(0, this.filters.length);
        this.filterChanged.emit({ filter: this.filters });
    }
       

    private selectedFieldType(field: string) {
        let res = this.fields.filter(f => f.datafield == field);
        if (res != null && res.length > 0) return res[0].columntype;
        return "";
    }

    private fieldFilters(field: string) {
        let res = this.fields.filter(f => f.datafield == field);
        if (res != null && res.length > 0) return res[0].filteritems;
        return undefined;
    }

    private changeFilterField(target,filter) {        
        if (this.selectedFieldType(target.value) == "dropdownlist")
            filter.condition = "EQUAL";
        else
            filter.condition = "CONTAINS";
    }

    private addFilter() {
        this.filters.push(new GridFilterExpression());
    }

    private removeFilter(filter: GridFilterExpression) {        
        let index = this.filters.indexOf(filter);
        this.filters.splice(index, 1);        
    }

};