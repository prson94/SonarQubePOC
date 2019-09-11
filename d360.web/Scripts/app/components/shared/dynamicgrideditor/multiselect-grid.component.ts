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
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="items" [selectionMode]="multiple ? 'multiple' : 'single'" [selection]="selectedItems" (selectionChange)="handleItemSelection($event);" [globalFilterFields]="['Text']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                        <ng-template pTemplate="header">
                            <tr>
                                <th style="width: 38px">
                                    <p-tableHeaderCheckbox *ngIf="multiple"></p-tableHeaderCheckbox>
                                </th>
                                <th>Name</th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">
                                <td>
                                        <p-tableRadioButton *ngIf="!multiple" [value]="item"></p-tableRadioButton>
                                        <p-tableCheckbox *ngIf="multiple" [value]="item"></p-tableCheckbox>
                                </td>
                                <td>
                                    <d3s-preview-tooltip [objectType]="getObjectTypeForTooltip(item)" [objectId]="getObjectIdForTooltip(item)">{{item.Text}}</d3s-preview-tooltip>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            <div *ngIf="selectedItems && selectedItems.length > 0" class="multiselect-grid-sel">Selected Items:
                                <p *ngIf="selectedItems && selectedItems.length > 0"><span *ngFor="let item of selectedItems;let last = last" >{{last?item.Text:item.Text +','}} </span></p>
                            </div>
                        </ng-template>
                    </p-table>
                </span>
                `,
    providers: [MULTISELECT_GRID_VALUE_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush, 
})

export class MultiSelectGridComponent extends BaseComponent implements OnInit, ControlValueAccessor  {   
    @Input() field: EditorField;
    @Input() multiple: boolean = true;

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

    private getObjectTypeForTooltip(item: any): string {
        if (item.Value.indexOf('|') == -1) return item.ObjectType;

        return item.Value.split('|')[0];
    }
    private getObjectIdForTooltip(item: any): number {
        if (item.Value.indexOf('|') == -1) return item.Value;

        return item.Value.split('|')[1];
    }

    private load() {
        this.isLoading = true;
        this.uriBasedService.getItems(this.field.TypeaheadUri).
            subscribe(result => {
                this.items = result;
                this.isLoading = false;
                this.ref.markForCheck();
            });
    }

    private handleItemSelection(event) {
        if (this.multiple) {
            this.selectedItems = event;
            var items = [];
            for (let item of event) {
                items.push(item.Value);
            }
            this.value = _.cloneDeep(items);
            this.onModelChange(this.value);
        }
        else {
            var items = [];
            items.push(event.Value);
            var sel = [];
            sel.push(event);
            this.selectedItems = sel;
            this.value = _.cloneDeep(items);
            this.onModelChange(this.value);
        }
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