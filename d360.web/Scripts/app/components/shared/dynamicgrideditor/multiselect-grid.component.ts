import { Input, Component, Output, EventEmitter, OnInit, forwardRef, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../base.component';
import { UriBasedService } from '../../../services/uri-based.service';
import { EditorField } from '../../../models/editor-field.model';
import * as _ from 'lodash';
import {NG_VALUE_ACCESSOR, ControlValueAccessor} from '@angular/forms';

export const MULTISELECT_GRID_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => MultiSelectGridComponent),
    multi: true
};

@Component({
    selector: 'd3s-multiselect-grid',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading">
                    <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter" (keypress)="ref.markForCheck()">
                    <p-dataTable #dt [globalFilter]="gb" [value]="items" [selection]="selectedItems" (selectionChange)="selectedItems=$event;handleItemSelection($event);" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">                    
                        <p-column [style]="{'width':'38px'}" selectionMode="multiple"></p-column>
                        <p-column field="Text" header="Name">
                            <template let-item="rowData" pTemplate type="body">
                                <d3s-tooltip [objectType]="item.Value.split('|')[0]" [objectId]="item.Value.split('|')[1]" tooltipType="preview">{{item.Text}}</d3s-tooltip>
                            </template>
                        </p-column>                    
                        <footer>
                            <d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info>
                            <div *ngIf="selectedItems && selectedItems.length > 0" class="multiselect-grid-sel">Selected Items:
                                <p *ngIf="selectedItems && selectedItems.length > 0"><span *ngFor="let item of selectedItems;let last = last" >{{last?item.Text:item.Text +','}} </span></p>
                            </div>
                        </footer>
                     </p-dataTable>
                </span>
                `,
    providers: [MULTISELECT_GRID_VALUE_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush, 
})

export class MultiSelectGridComponent extends BaseComponent implements OnInit, ControlValueAccessor  {   
    @Input() field: EditorField;

    value: any; //stores the values array bound back to the ngform.

    items: any[];
    selectedItems: any;

    public onModelChange: Function = () => { };

    public onModelTouched: Function = () => { };

    constructor(private uriBasedService: UriBasedService, private ref: ChangeDetectorRef) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.uriBasedService.getItems(this.field.TypeaheadUri).
            then(result => {
                this.items = result;
                this.isLoading = false;
                this.ref.markForCheck();
            });
    }

    private handleItemSelection(event) {        
        var items = [];
        for (let item of event) {
            items.push(item.Value);
        }
        this.value = _.cloneDeep(items);
        this.onModelChange(this.value);
    }

    writeValue(value: any): void {
        this.value = value;
    }

    registerOnChange(fn: Function): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: Function): void {
        this.onModelTouched = fn;
    }
};