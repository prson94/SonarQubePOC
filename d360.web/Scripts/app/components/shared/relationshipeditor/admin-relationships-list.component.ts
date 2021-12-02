import { Input, Component, Output, EventEmitter, OnChanges, SimpleChange, ChangeDetectorRef } from '@angular/core';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipType } from '../../../models/relationship.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { PredicateFriendlyType } from '../../../models/predicate.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-relationships-list',
    providers: [RelationshipsService],
    template: `
                <header *ngIf="!showEditor && !showDelete">Relationship Types
                    <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasExport]="true" (exportClick)="export()"></d3s-tile-actions>
                </header>    
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div  *ngIf="!showEditor && !showDelete && !isLoading" class="row">                    
                    <div class="col s12">
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter"  [ngModel]="dt.filters['global']?.value">
                        <p-table #dt [value]="relationships" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Id','Subject.Name','Predicate.Name','Predicate.Inverse','Object.Name']" [pageLinks]="3" [paginator]="true" [rows]="20"  [selection]="selected" 
                            [stateStorage]="gridStateStorage" stateKey="{{gridStorageKey}}"
                            (selectionChange)="selected=$event;selectedChange.emit(selected)">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'Id'" style="width: 70px;">
                                        ID
                                        <d3s-sortIcon [field]="'Id'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Subject.Name'">
                                        Subject
                                        <d3s-sortIcon [field]="'Subject.Name'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Predicate.Name'">
                                        Predicate
                                        <d3s-sortIcon [field]="'Predicate.Name'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Object.Name'">
                                        Object
                                        <d3s-sortIcon [field]="'Object.Name'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th><d3s-column-filter [value]="dt.filters['Id']?.value" [field]="'Id'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [value]="dt.filters['Subject.Name']?.value" [field]="'Subject.Name'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [value]="dt.filters['Predicate.Name']?.value" [field]="'Predicate.Name'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [value]="dt.filters['Object.Name']?.value" [field]="'Object.Name'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr (dblclick)="selected=item;selectedChange.emit(selected);showEditor=true;" [pSelectableRow]="item">
                                    <td>{{item.Id}}</td>
                                    <td>
                                        <span>{{item?.Subject.Name}}<span style="color: #999;font-size:75%;"> ({{item?.Subject.Class}})</span></span>
                                    </td>
                                    <td>
                                        <span *ngIf="item.Predicate.Name && item.Predicate.Inverse">{{item.Predicate.Name}} / {{item.Predicate.Inverse}} <span style="color: #999;font-size:75%;">({{getFriendlyNameForFunctionalType(item.Predicate.Type)}})</span></span>
                                    </td>
                                    <td>
                                        <span>{{item?.Object.Name}}<span style="color: #999;font-size:75%;"> ({{item?.Object.Class}})</span></span>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <d3s-preview-tooltip objectType="IntersectType" [objectId]="item.Id" icon="info"></d3s-preview-tooltip>
                                        </div>
                                    </td>
                                    <td>
                                        <div *ngIf="item?.Predicate.Type != 'Diagram' && item?.Predicate.Type != 'DiagramUse' && item?.Predicate.Type != 'DiagramReference'" class="RowTools">
                                            <a style="cursor:pointer;" title="Download all relationships in this type" (click)="downloadRel(item)"><i class="fa fa-download"></i></a>
                                        </div>
                                    </td>
                                    <td>
                                        <div *ngIf="!item.IsSystem" class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;selectedChange.emit(selected);showEditor=true"><i class="fa fa-pencil"></i></a>
                                        </div>
                                    </td>
                                    <td>
                                        <div *ngIf="!item.IsSystem" class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;selectedChange.emit(selected);showDelete=true"><i class="fa fa-trash-o"></i></a>
                                        </div>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>
                    </div>
                </div>
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.Id"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the relationship [' + [selected?.Subject.Name] + ' / ' + [selected?.Object.Name]  + ']?'"
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>  
                <d3s-admin-relationships-editor *ngIf="showEditor" [relationshipID]="selected?.Id" (saveClick)="saveRelationship($event)" (closeClick)="closeEditor()"></d3s-admin-relationships-editor>
            `
})

export class AdminRelationshipsListComponent extends BaseComponent implements OnChanges {
    relationships: RelationshipType[] = [];

    @Input() filterToName: string;

    @Input() objectType: string;
    @Input() objectID: number;

    @Input() selected: RelationshipType;
    @Output() selectedChange = new EventEmitter();

    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    private gridStorageKey: string = "admin-relationships-grid";

    constructor(
        private messagesService: MessagesObservableService,
        private relationshipsService: RelationshipsService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
        this.theDeleteCallback = this.deleteRelationship.bind(this);
    }

    ngOnInit() {
        this.getRelationships();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes['filterToName'] && changes['filterToName'].currentValue != changes['filterToName'].previousValue) || (changes['objectID'] && changes['objectID'].currentValue != changes['objectID'].previousValue)) {
            this.getRelationships();
        }
    }

    private updateStorageKey() {
        if (this.objectType && this.objectID) {
            this.gridStorageKey = `admin-relationships-grid_${this.objectType}_${this.objectID}`;
        }
    }

    private export() {
        this.relationshipsService.exportRelationshipTypes();
    }

    private filterResults() {
        if (this.filterToName && this.filterToName.length > 0) {
            var search = this.filterToName.toLowerCase();
            this.relationships = this.relationships.filter(item =>
                item.Predicate && item.Predicate.Name && item.Predicate.Name.toLowerCase().includes(search) ||
                item.Predicate && item.Predicate.Inverse && item.Predicate.Inverse.toLowerCase().includes(search) ||
                item.Object && item.Object.Class && item.Object.Class.toLowerCase().includes(search) ||
                item.Object && item.Object.Name && item.Object.Name.toLowerCase().includes(search) ||
                item.Subject && item.Subject.Class && item.Subject.Class.toLowerCase().includes(search) ||
                item.Subject && item.Subject.Name && item.Subject.Name.toLowerCase().includes(search)
            );
        }
    }

    getFriendlyNameForFunctionalType(type: string): string {
        let friendly: string = type;

        friendly = PredicateFriendlyType[type];

        return friendly;
    }

    getRelationships() {
        this.updateStorageKey();
        this.isLoading = true;
        if (this.objectID && this.objectType) {
            this.relationshipsService.getRelationshipTypesById(this.objectID, this.objectType)
                .subscribe(result => {
                    this.relationships = result ?? [];
                    this.isLoading = false;
                    if (this.relationships) {
                        if (this.relationships.length > 0) {
                            this.selected = this.relationships[0];
                            this.selectedChange.emit(this.selected)
                        }
                    }
                    this.checkGridState();
                });
        } else {
            this.relationshipsService.getRelationshipTypes()
                .subscribe(result => {
                    this.relationships = result ?? [];
                    this.filterResults();
                    this.isLoading = false;
                    if (this.relationships) {
                        if (this.relationships.length > 0) {
                            this.selected = this.relationships[0];
                            this.selectedChange.emit(this.selected)
                        }
                    }
                    this.checkGridState();
                });
        }
    }

    private checkGridState() {
        if (sessionStorage.getItem(this.gridStorageKey)) {
            let gridData = JSON.parse(sessionStorage.getItem(this.gridStorageKey));

            if (gridData.filters && Object.keys(gridData.filters).filter(x => x != "global").length > 0)
                this.showSimpleFilter = false;

            this.cdRef.detectChanges();
        }
    }

    findRelationshipIndex(id: number) {
        var index: number = -1;
        for (var relationship of this.relationships) {
            index++;
            if (relationship.Id == id) return index;
        }
    }

    deleteRelationship(id: number) {
        this.relationshipsService.deleteRelationship(id)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
                    this.relationships.splice(this.findRelationshipIndex(id), 1);
                }
            });
    }

    saveRelationship(event) {
        this.relationshipsService.saveRelationship(event.relationship)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type == "confirm") {
                    this.getRelationships();
                    this.showEditor = false;
                }
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    displayTypeName(type: string) {
        if (!type) return "";
        return type.replace("Type", "");
    }

    public downloadRel(relationship: RelationshipType) {
        this.relationshipsService.exportRelationshipTypeItems(relationship);
    }
}
