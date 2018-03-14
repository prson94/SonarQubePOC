import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { GridFilterExpression, GridFilterColumn, GridFilterFieldType } from '../../models/grid-definition.model';
import { FieldsService } from '../../services/fields.service';

@Component({
    selector: 'd3s-artifact-top-level-filter',    
    template: ` 
                <form (ngSubmit)="onSubmit()" #filterForm="ngForm">
                    <div class="row">
                        <ng-template ngFor let-field [ngForOf]="fields">                                                
                            <div [ngSwitch]="field.columntype" class="col s3">                                                
                                <div class="row">
                                    <div class="col s12 FieldName">{{field.text}}</div>
                                    <div class="col s12">
                                        <span *ngSwitchCase="'dropdownlist'">                                            
                                            <p-multiSelect [name]="field.datafield" [options]="field.filteritems | arraySelectItemPipe" [ngModel]="field.value" (ngModelChange)="field.value=$event;enableParentFilters(field);" [style]="{width:'100%'}" [disabled]="field.disabled"></p-multiSelect>
                                        </span>           
                                        <span *ngSwitchCase="'datetimeinput'">                            
                                            <p-calendar [(ngModel)]="field.value" [name]="field.datafield" [dateFormat]="getLocaleDateString()"></p-calendar>
                                        </span>
                                        <input *ngSwitchDefault [name]="field.datafield" type="text" [ngModel]="field.value" (ngModelChange)="field.value = $event" placeholder="Enter a value" style="width:100%;">                                 
                                    </div>
                                </div>
                            </div>
                        </ng-template>                        
                        <div class="col s6 buttons">
                            <div class="row">
                                <div class="col s12">&nbsp;</div>
                                <div class="col s12">
                                    <button pButton type="submit" [disabled]="!filterForm.form.valid" style="padding:3px" label="Filter Results"></button>
                                    &nbsp;
                                    <button pButton (click)="resetFilters()" type="button" style="padding:3px" label="Clear Filters"></button>
                                </div>                                
                            </div>
                        </div>
                    </div>                    
                    <br/>
                </form>
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsService]
})


export class ArtifactTopLevelFilterComponent extends BaseComponent implements OnInit {
    @Input() fields: GridFilterColumn[];
    
    @Output() filterChanged = new EventEmitter();

    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();

    constructor(protected ref: ChangeDetectorRef, protected fieldService: FieldsService) {
        super();        
    }

    ngOnInit() {
        for (let field of this.fields) {
            if (field.parentFieldTypeID > 0) field.disabled = true;
            else field.disabled = false;
        }        
        this.ref.markForCheck();
    }
    
    private resetFilters(): void {
        this.filters = [];
        for (let field of this.fields) {            
            if (!field.value || field.value === '') continue;
            field.value = null;
        }
        this.filtersChange.emit(this.filters);
        this.filterChanged.emit();        
    }

    onSubmit() {
        this.filters = [];
        //copy field values to filter values
        for (let field of this.fields) {
            if (!field.value || field.value === '') continue;
            let filter = new GridFilterExpression();
            filter.field = field.datafield;
            
            if (field.columntype == "dropdownlist") {
                let newVal = '';
                if (field.value.length > 0) {                    
                    for (let item of field.value) {
                        if (newVal.length > 0) newVal += '!~!';
                        newVal += item;
                    }                    
                    filter.value = newVal;
                    filter.condition = "IN";
                    filter.fieldtype = (field.hiddenfield) ? GridFilterFieldType.Hidden : GridFilterFieldType.Normal;
                    this.filters.push(filter);
                }                
            }
            else if (field.columntype == "datetimeinput") {
                filter.condition = "EQUALS";
                
                var date = new Date(field.value);                
                filter.value = date.getMonth() + 1 + "/" + date.getDate() + "/" + date.getFullYear();
                filter.fieldtype = (field.hiddenfield) ? GridFilterFieldType.Hidden : GridFilterFieldType.Normal;
                this.filters.push(filter);
            }
            else {
                filter.condition = "CONTAINS";
                filter.value = field.value;
                filter.fieldtype = (field.hiddenfield) ? GridFilterFieldType.Hidden : GridFilterFieldType.Normal;
                this.filters.push(filter);
            }            
        }
        
        this.filtersChange.emit(this.filters);
        this.filterChanged.emit();        
    }    

    public enableParentFilters(givenfield: GridFilterColumn): void {
        for (let field of this.fields) {
            if (`Field${field.parentFieldTypeID}` == givenfield.datafield)
            {
                this.loadFieldItems(givenfield, field);                
            }
        }
    }

    public loadFieldItems(givenparentfield: GridFilterColumn, givenfield: GridFilterColumn): void {
        var fieldId = +givenfield.datafield.replace('Field', '');
        if (givenparentfield.value.length > 0) {
            this.fieldService.getCascadingListFieldValues(fieldId, undefined, givenparentfield.value).then(res => {
                givenfield.disabled = false;
                givenfield.filteritems = res.map(r => r.Text);

                this.ref.markForCheck();
            })
        }
        else {
            givenfield.disabled = true;
            givenfield.filteritems = [];
            this.ref.markForCheck();
        }        
    }

};