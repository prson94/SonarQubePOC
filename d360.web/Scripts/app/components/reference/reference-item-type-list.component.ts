import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ReferenceService } from '../../services/reference.service';
import { MessagesService } from '../../services/messages.service';
import { PermissionsService } from '../../services/permissions.service';
import { ReferenceItemType } from '../../models/reference.model';
import { FormMode } from '../../models/form.model';
import { AssetTypeService } from '../../services/asset-type.services';


@Component({
    selector: 'd3s-reference-item-type-list',
    template: ` 
                <div class="tile tile-detail">
                    <header *ngIf="!showEditor">Reference Lists
                        <d3s-tile-actions [hasAdd]="!showDelete && hasModifyAssetPermissions()" (addClick)="selected=null;showEditor=true;"></d3s-tile-actions>                            
                    </header>                    
                    <span *ngIf="!showEditor && !showDelete">
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                        <p-table #dt [value]="referenceTypes" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions"   [selection]="selected" (selectionChange)="selected=$event;selectedChange.emit(selected);">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th style="width: 28px" *ngIf="hasModifyAssetPermissions()"></th>
                                <th style="width: 28px" *ngIf="hasDeleteAssetPermissions()"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">
                                <td>{{item.Name}}</td>
                                <td *ngIf="hasModifyAssetPermissions()">
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td *ngIf="hasDeleteAssetPermissions()"> 
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                    </span>                 
                    <d3s-asset-type-editor *ngIf="showEditor"                             
                            [assetTypeClass]="'RT'"                             
                            [id]="selected?.AssetTypeID" 
                            [title]="selected != null ? 'Edit Reference List' : 'Add Reference List'" 
                            (onCancel)="showEditor = false;" 
                            (onComplete)="saveReferenceItemType($event)">
                       </d3s-asset-type-editor>
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.AssetTypeID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the selected item?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form>  
                </div>
              `,
    providers: [ReferenceService, PermissionsService,AssetTypeService],
})

export class ReferenceItemTypeGridComponent extends BaseComponent implements OnInit {
    @Input() selected: ReferenceItemType;
    @Output() selectedChange = new EventEmitter();

    @Input() initialSelectedListId: number;

    private referenceTypes: ReferenceItemType[];
    private _showEditor: boolean = false;
    private _showDelete: boolean = false;

    @Output() formModeChange = new EventEmitter<FormMode>();
    private get showEditor(): boolean {
        return this._showEditor;
    }

    private set showEditor(value: boolean) {
        if (value != this._showEditor && value) this.formModeChange.emit(FormMode.Editing | FormMode.Adding);
            
        this._showEditor = value;

        if (!this._showDelete && !this._showEditor) this.formModeChange.emit(FormMode.Default);
    }

    private get showDelete(): boolean {
        return this._showDelete;
    }


    private set showDelete(value: boolean) {
        if (value != this._showDelete && value) this.formModeChange.emit(FormMode.Deleting);

        this._showDelete = value;

        if (!this._showDelete && !this._showEditor) this.formModeChange.emit(FormMode.Default);
    }

    theDeleteCallback: Function;
    
    constructor(private referenceService: ReferenceService,
        private permissionsService: PermissionsService,
        private assetTypeService: AssetTypeService,
        private messagesService: MessagesService) {
        super();
        this.showDelete = false;
        this.showEditor = false;
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
        this.assetTypeService.deleteAssetType(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    let index = this.referenceTypes.findIndex(x => x.AssetTypeID == id);
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
        this.showEditor = false;
        if (event.id) this.initialSelectedListId = (0 + event.id);
        this.load();
    }
};