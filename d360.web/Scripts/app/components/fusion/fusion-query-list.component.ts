import {Component, EventEmitter, Input, OnChanges, Output, SimpleChange} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionConfigurationDetails, FusionQueryAttributeType} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';
import {MessagesService} from '../../services/messages.service';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-query-list',
    template: `
        <div class="col s12">
            <div class="tile tile-detail">
                <header *ngIf="!isLoading && !showDelete && !showEditor">Fusion Queries For {{fusion?.Name}}
                    <d3s-tile-actions [hasAdd]="true" (addClick)="showAddQuery()" [hasFilterMode]="true"
                                      [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">                                               
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                               (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                               class="grid-simple-filter">
                        <p-table #dt [value]="queries" selectionMode="single" [metaKeySelection]="true"
                                 [globalFilterFields]="['Name','Uri']" [pageLinks]="3" [paginator]="true"
                                 [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions"
                                 [(selection)]="selected">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'Name'">
                                        Name
                                        <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                    </th>
                                    <th>Uri</th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th><d3s-column-filter [field]="'Name'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Uri'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th></th>
                                    <th></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr (dblclick)="selected=item" [pSelectableRow]="item">
                                    <td>{{item.Name}}</td>
                                    <td>
                                        <a target="_blank"
                                           href="/services/fusion/{{item.FusionID}}/{{item.ID}}/data?metadata=true">/services/fusion/{{item.FusionID}}
                                            /{{item.ID}}/data?metadata=true</a>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i
                                                    class="fa fa-pencil"></i></a>
                                        </div>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i
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
                <d3s-delete-form *ngIf="showDelete"
                                 [callback]="theDeleteCallback"
                                 [itemId]="selected?.ID"
                                 [method]="'callback'"
                                 [prompt]="'Are you sure you want to delete the query [' + [selected?.Name] + ']?'"
                                 (onCancel)="showDelete=false;"
                ></d3s-delete-form>
                <d3s-fusion-query-attribute-editor *ngIf="showEditor" [query]="selected" (saveClick)="doSave($event);"
                                                   (closeClick)="showEditor=false;"></d3s-fusion-query-attribute-editor>
            </div>
        </div>
        <div class="col s12" *ngIf="!isLoading && !showDelete && !showEditor && selected">
            <div class="tile tile-detail">
                <d3s-field-definition-tile [objectType]="'FusionQueryAttributeType'"
                                           [objectID]="selected.ID"></d3s-field-definition-tile>
            </div>
        </div>
    `,
    providers: [FusionService],
})

export class FusionQueryListComponent extends BaseComponent implements OnChanges {
    @Input() fusion: FusionConfigurationDetails;
    @Output() treeRequiresUpdate = new EventEmitter();

    private queries: FusionQueryAttributeType[] = [];
    private selected: FusionQueryAttributeType;

    private showDelete: boolean = false;
    private showEditor: boolean = false;

    destroySubject$: Subject<void> = new Subject();

    public theDeleteCallback: Function;

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();

        this.theDeleteCallback = this.deleteQuery.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['fusion'] && this.fusion) {
            this.load();
        }
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionQueryAttributeTypes(this.fusion.FusionTypeID, this.fusion.ID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                result => {
                    this.queries = result;
                    this.selected = this.queries.length > 0 ? this.queries[0] : null;

                    this.isLoading = false;
                }
            );
    }

    private showAddQuery() {
        this.selected = null;
        this.showEditor = true;
    }

    private doSave(data) {
        data.query.FusionID = this.fusion.ID;

        this.fusionService
            .saveQueryAttributeType(data.query)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);

                    if (result.type != 'error') {
                        if (data.query.ID == undefined) {
                            data.query.ID = Number(result.id);
                            this.queries[this.queries.length] = data.query;
                            this.treeRequiresUpdate.emit();
                        } else {
                            let index = this.queries.findIndex(x => x.ID == data.query.ID);

                            if (index >= 0 && index < this.queries.length)
                                this.queries[index] = data.query;
                        }

                        this.selected = data.query;
                    }

                    this.showEditor = false;
                }
            )
        ;
    }

    private deleteQuery(id: number) {
        this.fusionService
            .deleteFusionQuery(id)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);

                    //remove the template with this id from the grid
                    if (result.type != 'error') {
                        this.queries = this.queries.filter(x => x.ID != id);
                        this.selected = this.queries.length > 0 ? this.queries[0] : null;
                    }

                    this.showDelete = false;
                    this.treeRequiresUpdate.emit();
                }
            )
        ;
    }
}
