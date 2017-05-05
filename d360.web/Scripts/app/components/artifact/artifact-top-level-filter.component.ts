import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { GridFilterExpression, GridFilterColumn, GridFilterFieldType } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-artifact-top-level-filter',    
    template: ` 
                <form (ngSubmit)="onSubmit()" #filterForm="ngForm">
                    <div class="row">
                        <template ngFor let-field [ngForOf]="fields">                                                
                            <div [ngSwitch]="field.columntype" class="col s3">                                                
                                <div class="row">
                                    <div class="col s12 FieldName">{{field.text}}</div>
                                    <div class="col s12">
                                        <span *ngSwitchCase="'dropdownlist'">
                                            <select [name]="'FilterValue_' + index" [ngModel]="field.value" (ngModelChange)="field.value = $event" style="width:100%;" placeholder="Choose a field">                                            
                                                <option value=""></option>
                                                <option *ngFor="let p of field.filteritems" [value]="p">{{p}}</option>
                                            </select>
                                        </span>                                
                                        <input *ngSwitchDefault [name]="'FilterValue_' + index" type="text" [ngModel]="field.value" (ngModelChange)="field.value = $event" placeholder="Enter a value" style="width:100%;">                                 
                                    </div>
                                </div>
                            </div>
                        </template>
                        <div class="col s3 buttons">
                            <div class="row">
                                <div class="col s12">&nbsp;</div>
                                <div class="col s12">
                                    <button pButton type="submit" [disabled]="!filterForm.form.valid" style="width: '150px';padding:3px" label="Filter Results"></button>                            
                                </div>
                            </div>
                        </div>
                    </div>                    
                    <br/>
                </form>
                `
})


export class ArtifactTopLevelFilterComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() fields: GridFilterColumn[];
    
    @Output() filterChanged = new EventEmitter();

    @Input() filters: GridFilterExpression[] = [];
    @Output() filtersChange = new EventEmitter();

    constructor() {
        super();        
    }

    ngOnInit() {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        
    }

    onSubmit() {
        this.filters = [];
        //copy field values to filter values
        for (let field of this.fields) {
            if (!field.value || field.value === '') continue;
            let filter = new GridFilterExpression();
            filter.field = field.datafield;
            filter.value = field.value;

            if (field.columntype == "dropdownlist")
                filter.condition = "EQUAL";
            else
                filter.condition = "CONTAINS";

            filter.fieldtype = (field.hiddenfield) ? GridFilterFieldType.Hidden : GridFilterFieldType.Normal;
            
            this.filters.push(filter);
        }
        
        this.filtersChange.emit(this.filters);
        this.filterChanged.emit();        
    }
};