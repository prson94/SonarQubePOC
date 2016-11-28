import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService, ReferenceService, MessagesService, PermissionsService } from '../../services/index';
import { ReferenceItemType } from '../../models/reference.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-reference-item-type-list',
    template: ` 
                <div class="tile tile-detail">
                    <header *ngIf="!showEditor">Reference Types
                        <d3s-tile-actions [hasAdd]="!showDelete && hasRootCreatePermissions()" (addClick)="selected=null;showEditor=true;"></d3s-tile-actions>                            
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading && !showEditor && !showDelete">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                        <p-dataTable #dt [globalFilter]="gb" [value]="referenceTypes" selectionMode="single" [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected);" scrollable="true" scrollWidth="100%" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">                                                
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="Name" header="Name" [sortable]="true"></p-column>                                
                            <p-column [style]="{width:'28px'}" *ngIf="hasRootUpdatePermissions()">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                    </div>
                                </template>
                            </p-column>                            
                            <p-column  [style]="{width:'28px'}" *ngIf="hasRootDeletePermissions()">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                    </div>
                                </template>
                            </p-column>       
                        </p-dataTable>  
                    </span>
                    <d3s-reference-item-type-editor *ngIf="showEditor" [referenceItemType]="selected" (closeClick)="showEditor = false;" (saveClick)="saveReferenceItemType($event)"></d3s-reference-item-type-editor>
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.ID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the selected item?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form>  
                </div>
              `,
    providers: [ReferenceService, PermissionsService],
})

export class ReferenceItemTypeGridComponent extends BaseComponent implements OnInit {
    @Input() selected: ReferenceItemType;
    @Output() selectedChange = new EventEmitter();

    @Input() initialSelectedListId: number;

    private referenceTypes: ReferenceItemType[];
    private showEditor: boolean = false;
    private showDelete: boolean = false;

    theDeleteCallback: Function;
    
    constructor(private referenceService: ReferenceService,
        private permissionsService: PermissionsService,
        private messagesService: MessagesService) {
        super();

        this.theDeleteCallback = this.deleteReferenceItemType.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);
        this.referenceService.getReferenceItemTypes()
            .then(result => {
                this.referenceTypes = result;
                if (this.referenceTypes.length > 0) {
                    if (this.initialSelectedListId > 0) {
                        console.log('here');
                        let index = this.referenceTypes.findIndex(x => x.ID == this.initialSelectedListId);
                        this.initialSelectedListId = 0;
                        if (index >= 0 && index < this.referenceTypes.length) {
                            this.selected = this.referenceTypes[index];
                        }
                        else {
                            this.selected = this.referenceTypes[0];
                        }
                    }
                    else {
                        this.selected = this.referenceTypes[0];
                    }
                    this.selectedChange.emit(this.selected);
                }
                this.isLoading = false;
            });
    }

    private deleteReferenceItemType(id: number) {
        this.isLoading = true;
        this.referenceService.deleteReferenceItemType(id).then(
            result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {                    
                    let index = this.referenceTypes.findIndex(x => x.ID == id);
                    if (index >= 0 && index < this.referenceTypes.length) {
                        this.referenceTypes.splice(index, 1);
                    }
                    if (this.referenceTypes.length > 0) {
                        this.selected = this.referenceTypes[0];
                        this.selectedChange.emit(this.selected);
                    }
                }
                this.isLoading = false;
                this.showDelete = false;
            });        
    }

    private saveReferenceItemType(event) {
        this.isLoading = true;
        this.referenceService.saveReferenceItemType(event.referenceItemType)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {                
                    if (event.referenceItemType.ID == undefined) {
                        event.referenceItemType.ID = Number(result.id);
                        this.referenceTypes[this.referenceTypes.length] = event.referenceItemType;                        
                    }
                    else {
                        let index = this.referenceTypes.findIndex(x => x.ID == event.referenceItemType.ID);
                        if (index >= 0 && index < this.referenceTypes.length) {                            
                            this.referenceTypes[index] = event.referenceItemType;                            
                        }
                    }                    
                    this.selected = event.referenceItemType;
                    this.selectedChange.emit(this.selected);
                }
                this.isLoading = false;
                this.showEditor = false;
            });        
    }
};