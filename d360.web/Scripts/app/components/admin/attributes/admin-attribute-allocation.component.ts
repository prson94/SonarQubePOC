import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { AttributeTypeService } from '../../../services/attribute-type.service';
import { BaseComponent } from '../../shared/base.component';
import { AttributeType, AttributeTypeAllocation } from '../../../models/attribute-type.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-attribute-allocation',
    providers: [],
    template: `
               <header *ngIf="!showEditor && !showDelete">Allocations
                    <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showEditor && !showDelete">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="allocations" selectionMode="single" [globalFilterFields]="['ObjectType','ObjectName','AllowMultipleEntries']" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'ObjectType'">Object Type <d3s-sortIcon [field]="'ObjectType'"></d3s-sortIcon></th>
                                <th [pSortableColumn]="'ObjectName'">Object Name <d3s-sortIcon [field]="'ObjectName'"></d3s-sortIcon></th>
                                <th [pSortableColumn]="'AllowMultipleEntries'">Allow Multiple Entries <d3s-sortIcon [field]="'AllowMultipleEntries'"></d3s-sortIcon></th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'ObjectType'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'ObjectName'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'AllowMultipleEntries'" [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;showEditor=true;" [pSelectableRow]="item">
                                <td>{{item.ObjectType}}</td>
                                <td>{{item.ObjectName}}</td>
                                <td>
                                    <i *ngIf="item.AllowMultipleEntries" class="fa fa-check enabled" title="Allowed"></i>
                                    <i *ngIf="!item.AllowMultipleEntries" class="fa fa-times disabled" title="Not Allowed"></i>
                                </td>
                                <td>
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;editItem();"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>                      
                </span>
                <d3s-dynamic-editor *ngIf="showEditor" rowID="ObjectID" [editParams]="editParams" [parentID]="attributeID" [objectID]="selected?.ObjectID" objectType="AttributeAllocation" title="Attribute Allocation" [selection]="selected" (saveClick)="saveAllocation($event)" (closeClick)="this.showEditor = false;"></d3s-dynamic-editor>     
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the attribute allocation to [' + [selected?.ObjectName] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>  
                `
})

export class AdminAttributeAllocationComponent extends BaseComponent {
    @Input() attributeID: number;

    private selected: AttributeTypeAllocation;
    private allocations: AttributeTypeAllocation[] = [];
    private showEditor: boolean;
    private showDelete: boolean;
    private editParams: any[];
    theDeleteCallback: Function;

    constructor(private messagesService: MessagesService, private attributeTypeService: AttributeTypeService) {
        super();    
        this.theDeleteCallback = this.deleteAttributeAllocation.bind(this);    
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.attributeID != null) this.load();
    }

    private load() {
        this.isLoading = true;
        this.attributeTypeService.getAttributeTypeAllocations(this.attributeID)
            .then(result => {
                this.allocations = result;
                this.isLoading = false;                
            });
    }

    private editItem() {
        this.editParams = [];
        this.editParams.push(this.attributeID);
        this.editParams.push(this.selected.ObjectType);
        this.editParams.push(this.selected.ObjectID);
        this.showEditor = true;
    }

    private deleteAttributeAllocation(id: number) {
        this.isLoading = true;
        this.attributeTypeService.deleteAttributeTypeAllocations(this.attributeID, this.selected.ObjectID, this.selected.ObjectType)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    let index = this.allocations.findIndex(x => (x.ObjectID == this.selected.ObjectID && x.ObjectType == this.selected.ObjectType));
                    if (index >= 0 && index < this.allocations.length)
                        this.allocations.splice(index, 1);
                }
                this.showDelete = false;
                this.isLoading = false;
            });
    }

    private saveAllocation(data) {
        if (data.action == 'new') {
            this.isLoading = true;
            this.attributeTypeService.addAttributeTypeAllocations(data.item.ObjectTypeInfo, data.item.AllowMultipleEntries, this.attributeID).
                then(result => {
                    this.showMessageForResult(this.messagesService, result);

                    if (result.type != 'error') {
                        this.load();
                    }
                    this.isLoading = false;
                    this.showEditor = false;
                });

        }
        else {
            this.isLoading = true;
            this.attributeTypeService.editAttributeTypeAllocations(data.item.ObjectTypeInfo, data.item.AllowMultipleEntries, this.attributeID).
                then(result => {
                    this.showMessageForResult(this.messagesService, result);

                    if (result.type != 'error') {
                        this.load();
                    }
                    this.isLoading = false;
                    this.showEditor = false;
                });

        }
        this.showEditor = false;
    }
    
    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.allocations = _.orderBy(this.allocations, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }

    add() {
        this.showEditor = true;
        this.editParams = [];
        this.selected = null;
    }
}


