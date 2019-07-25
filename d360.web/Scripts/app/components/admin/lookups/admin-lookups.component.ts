import {Component, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';

import {Lookup} from '../../../models/lookup.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {RightSidebarService} from '../../../services/right-sidebar.service';
import {LookupService} from '../../../services/lookup.service';

import {AdminBaseComponent} from '../admin-base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-admin-lookups-component',
    providers: [LookupService],
    template: `
        <div class="row">
            <div class="col l4 s12">
                <div class="tile tile-detail">
                    <header *ngIf="!showEditor && !showDelete">Lookup Types
                        <d3s-tile-actions [hasAdd]="true"
                                          (addClick)="add()"></d3s-tile-actions>
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!showEditor && !showDelete && !isLoading">       
                                <input type="text"
                                       [hidden]="!showSimpleFilter"
                                       pInputText
                                       size="100"
                                       (input)="dt.filterGlobal($event.target.value, 'contains')"
                                       placeholder="Search..."
                                       class="grid-simple-filter">
                                <p-table #dt
                                         [value]="lookups"
                                         selectionMode="single"
                                         [metaKeySelection]="true"
                                         [globalFilterFields]="['ID','Name']"
                                         [pageLinks]="3"
                                         [paginator]="true"
                                         [rows]="20"
                                         [(selection)]="selectedLookup">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'ID'">
                                                ID
                                                <d3s-sortIcon [field]="'ID'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body"
                                                 let-item>
                                        <tr (dblclick)="selectedLookup=item;showEditor=true;"
                                            [pSelectableRow]="item">
                                            <td>{{item.ID}}</td>
                                            <td>{{item.Name}}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;"
                                                       (click)="selectedLookup=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;"
                                                       (click)="selectedLookup=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords"
                                                 pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first"
                                                              [rows]="dt.rows"
                                                              [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>
                            </span>
                    <d3s-delete-form *ngIf="showDelete"
                                     [callback]="theDeleteCallback"
                                     [itemId]="selectedLookup?.ID"
                                     [method]="'callback'"
                                     [prompt]="'Are you sure you want to delete the lookup [' + [selectedLookup?.Name] + ']?'"
                                     (onCancel)="showDelete=false;"
                    ></d3s-delete-form>
                    <d3s-admin-lookup-type-editor *ngIf="showEditor"
                                                  [lookup]="selectedLookup"
                                                  (saveClick)="saveLookup($event)"
                                                  (closeClick)="closeEditor()"></d3s-admin-lookup-type-editor>
                </div>
            </div>
            <div class="col l8 s12"
                 *ngIf="!showDelete && !showEditor && selectedLookup">
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-field-definition-tile [objectType]="'LookupType'"
                                                       [objectID]="selectedLookup?.ID"></d3s-field-definition-tile>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-dynamic-grid [title]="'Items'"
                                              [itemName]="'Lookup'"
                                              [objectType]="'LookupType'"
                                              [objectID]="selectedLookup?.ID"
                                              [createUri]="'form/dynamicedit/create/lookup/'"
                                              [editUri]="'form/dynamicedit/edit/lookup/'"
                                              [dataUri]="lookupUri()"
                                              [deleteUri]="'form/DeleteLookupByIdRaw?id='"></d3s-dynamic-grid>
                        </div>
                    </div>
                </div>
                <div>
                </div>
    `
})

export class AdminLookupsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    private sub: any;
    lookups: Lookup[] = [];
    selectedLookup: Lookup;
    private selectedLookupTypeId: number = 0;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        private lookupService: LookupService,
        protected messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);

        this.areaName = "Lookup Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/LookupType/${this.selectedLookup.ID}`
            });
        }
    }

    ngOnInit() {
        this.theDeleteCallback = this.deleteLookup.bind(this);

        this.getLookups().subscribe(
            result => {
                this.lookups = result;

                if (this.lookups.length > 0) {
                    this.selectedLookup = this.lookups[0];
                }

                this.sub = this.route.params.subscribe(
                    params => {
                        this.selectedLookupTypeId = +params['lookupTypeId']; // (+) converts string 'id' to a number

                        let preselected = this.lookups.find(i => i.ID === this.selectedLookupTypeId);

                        if (preselected) {
                            this.selectedLookup = preselected;
                        }
                    }
                );

                this.isLoading = false;
            }
        );
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getLookups() {
        this.isLoading = true;

        return this.lookupService.getLookups();
    }

    deleteLookup(id: number) {
        this.isLoading = true;

        this.lookupService.deleteLookupType(id).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;

                if (result.type != 'error') {
                    this.selectedLookup = this.lookups.length > 0 ? this.lookups[0] : null;
                    this.lookups = this.lookups.filter(x => x.ID != id);
                }

                this.isLoading = false;
            }
        );
    }

    lookupUri() {
        if (this.selectedLookup == null) {
            return "";
        }

        return `resources/lookups/${this.selectedLookup.ID}/items.json`;
    }

    add() {
        this.showEditor = true;
        this.selectedLookup = null;
    }

    closeEditor() {
        this.showEditor = false;

        if (this.selectedLookup == null) {
            this.selectedLookup = this.lookups.length > 0 ? this.lookups[0] : null;
        }
    }

    saveLookup(event) {
        this.isLoading = true;
        this.lookupService.saveLookup(event.lookup).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type == "error") {
                    return;
                }

                if (event.lookup.ID == undefined) {
                    event.lookup.ID = Number(result.id);
                    this.lookups[this.lookups.length] = event.lookup;
                } else {
                    let index = this.lookups.findIndex(x => x.ID == event.lookup.ID);

                    if (index >= 0 && index < this.lookups.length) {
                        this.lookups[index] = event.lookup;
                    }
                }

                this.selectedLookup = event.lookup;

                this.showEditor = false;
                this.isLoading = false;
            }
        );
    }
}
