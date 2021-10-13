import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { UriBasedService } from '../../../services/uri-based.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-dynamic-grid',
    providers: [GridDefinitionService, UriBasedService],
    template: `
        <header *ngIf="!showEditor && !showDelete">{{title}}
            <d3s-tile-actions [hasAdd]="showAddButton" (addClick)="add()" hasFilterMode="true"
                              [(filterMode)]="showSimpleFilter" [hasExport]="showExportButton"
                              (exportClick)="exportClick.emit()"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                           (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                           class="grid-simple-filter">
                    <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true"
                             [globalFilterFields]="globalFilterFields" [sortField]="sortField" [pageLinks]="3"
                             [paginator]="true"
                             [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions"
                             [(selection)]="selected" (sortFunction)="customSort($event)" [customSort]="true">
                        <ng-template pTemplate="header">
                            <tr>
                                <th *ngFor="let column of columns"
                                    [pSortableColumn]="column.sortable ? column.datafield : null">
                                    {{column.text}}
                                    <d3s-sortIcon *ngIf="column.sortable" [field]="column.datafield"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th *ngFor="let column of columns">
                                    <d3s-column-filter [field]="column.datafield"
                                                       [datatype]="'text'"></d3s-column-filter>
                                </th>
                                <th></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;editItemClick.emit(selected)" [pSelectableRow]="item">
                                <td *ngFor="let column of columns">
                                    <d3s-dynamic-field-value [column]="column" [fields]="fields"
                                                             [item]="item"></d3s-dynamic-field-value>
                                </td>
                                <td>
                                    <div class="RowTools" *ngIf="item.UID">
                                        <a style="cursor:pointer;" (click)="selected=item;"><i
                                                [copy-clipboard]="item.UID"
                                                [pTooltip]="'UID: \n' + item.UID + '\n\n (click to copy)\n'"
                                                tooltipPosition="top" class="fa fa-info"></i></a>                                      
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools" *ngIf="showEditButton || item.P_CanEdit">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i
                                                class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools" *ngIf="showDeleteButton || item.P_CanDelete">
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i
                                                class="fa fa-trash-o"></i></a>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                                  [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                </span>
        <d3s-dynamic-editor *ngIf="showEditor" [objectID]="objectID" [objectType]="objectType"
                            [title]="itemName + ' Item'" [selection]="selected" [rowID]="rowID"
                            [selectedObject]="selectedObject" [selectedObjectID]="selected?.ID"
                            (saveClick)="saveItem($event)" (closeClick)="closeEditor()"
[objectTypeUid]="assetTypeUid" [isV2API]="useV2Api"></d3s-dynamic-editor>
        <d3s-delete-form *ngIf="showDelete && !assetTypeUid"
                         [callback]="theDeleteCallback"
                         [itemId]="selected?.ID"
                         [method]="'callback'"
                         [prompt]="'Are you sure you want to delete the selected item?'"
                         (onCancel)="showDelete=false;"
       ></d3s-delete-form>
        <d3s-asset-delete-editor *ngIf="showDelete && assetTypeUid"
                                     [uid]="selected?.UID"
                                     [assetTypeUid]="assetTypeUid"                                     
                                     (onCancel)="showDelete = false"
                                     (onDeleted)="onDeleted()">
        </d3s-asset-delete-editor>
    `
})

export class DynamicGridComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() rowID: string = 'ID';
    @Input() objectID: number;
    @Input() dataUri: string;
    @Input() deleteUri: string;
    @Input() createUri: string;
    @Input() editUri: string;
    @Input() title: string = "Items";
    @Input() itemName: string = "";
    @Input() sortField: string;
    @Input() assetTypeUid: string;

    @Input() showEditButton: boolean = true;
    @Input() showDeleteButton: boolean = true;
    @Input() showAddButton: boolean = true;
    @Input() showExportButton: boolean = false;
    @Input() useV2Api: boolean = false;


    @Output() editItemClick = new EventEmitter();
    @Output() exportClick = new EventEmitter();

    private selectedObject: string = '';
    error: any;
    items: any[] = [];
    columns: GridColumn[] = [];
    fields: GridField[] = [];

    showDelete: boolean = false;
    showEditor: boolean = false;

    selected: any = null;

    theDeleteCallback: Function;

    get globalFilterFields(): string[] {
        return this.columns.map((c) => c.datafield);
    }

    constructor(
        private gridDefinitionService: GridDefinitionService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private uriBasedService: UriBasedService) {
        super(settingsService);
        this.theDeleteCallback = this.deleteItem.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['objectID'] && changes['objectID'].previousValue != changes['objectID'].currentValue) {
            this.showEditor = false;
            this.showDelete = false;
        }
        if (this.objectID != null && this.objectType != null) this.load();
    }

    public load() {
        this.getFieldsDefinition();
        this.getData();

        if (this.objectType) {
            this.selectedObject = this.objectType.replace('Type', '');
        }
    }

    public onDeleted() {
        this.items = this.items.filter((x) => x.ID != this.selected.ID);
        this.selected = null;
        this.showDelete = false;
    }

    deleteItem(id: number) {
        this.uriBasedService.deleteItemWithResult(this.deleteUri, id)
            .subscribe((res) => {
                this.showMessageForResult(this.messagesService, res);
                this.showDelete = false;
                if (res.type != 'error')
                    this.items = this.items.filter((x) => x.ID != id);
            });
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.objectID, this.objectType).subscribe(
            result => {
                this.columns = result.Columns;
                this.fields = result.Fields;
            }
        );
    }

    getData() {
        this.isLoading = true;
        this.uriBasedService.getItems(this.dataUri)
            .subscribe((result) => {
                this.items = result;
                this.isLoading = false;
                if (this.items.length > 0) this.selected = this.items[0];
            });
    }

    doExport() {

    }

    closeEditor() {
        this.showEditor = false;
    }

    add() {
        this.selected = null;
        this.showEditor = true;
    }

    saveItem(event) {
        this.isLoading = true;
        if (!this.useV2Api) {
            this.uriBasedService.saveItem(this.createUri, this.editUri, event.item)
                .subscribe(result => {
                    this.showMessageForResult(this.messagesService, result);
                    //reload grid for now as the name / id of the field differs in display mode / edit mode
                    this.showEditor = false;
                    this.getData();
                });
        }
        else {
            this.showEditor = false;
            this.getData();
        }
    }

    customSort(e: any) {
        let field = e.field;
        let direction = e.order;

        var fld = this.fields.filter((x) => x.name == field);
        var type = (fld != null && fld.length > 0) ? fld[0].type : "";

        this.items = this.items.slice().sort((a, b) => {
            let fa = a[field];
            let fb = b[field];

            switch (type) {
                case 'number':
                    let na: number = +fa;
                    let nb: number = +fb;

                    if (na == null || isNaN(na))
                        na = -Infinity;
                    if (nb == null || isNaN(nb))
                        nb = -Infinity;

                    return ((na > nb) ? 1 : (na < nb) ? -1 : 0) * direction;
                case 'date':
                case 'datetime':
                    let da: number = Date.parse(fa);
                    let db: number = Date.parse(fb);

                    if (da == null || isNaN(da))
                        da = new Date(null).getTime();
                    if (db == null || isNaN(db))
                        db = new Date(null).getTime();

                    return ((da > db) ? 1 : (da < db) ? -1 : 0) * direction;
                default:
                    return ((fa > fb) ? 1 : (fa < fb) ? -1 : 0) * direction;
            }
        });

    }
}
