import { Component, OnInit, OnDestroy } from '@angular/core';
import { Taxonomy } from '../../../models/taxonomy.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { TaxonomiesService } from '../../../services/taxonomies.service';
import { FieldsService } from '../../../services/fields.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { FieldDefinition } from '../../../models/fields.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { AssetTypeService } from '../../../services/asset-type.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';

@Component({
    selector: 'd3s-admin-models-component',
    providers: [TaxonomiesService, FieldsService, AssetTypeService],
    template: `
        <div *ngIf="showEditor || showDelete && !isLoading" class="row">
            <div class="tile tile-detail">
                <d3s-asset-type-editor *ngIf="showEditor" [assetTypeClass]="assetTypeClass" [id]="selectedTaxonomy?.AssetTypeID"
                                       [title]="(selectedTaxonomy == null ? 'New' : 'Edit') + ' Model Type'"
                                       (onCancel)="closeEditor()"
                                       (onComplete)="saveModel($event)"></d3s-asset-type-editor>
                <d3s-delete-form *ngIf="showDelete"
                                 [callback]="theDeleteCallback"
                                 [itemId]="selectedTaxonomy?.AssetTypeID"
                                 [method]="'callback'"
                                 [prompt]="'Are you sure you want to delete the model [' + [selectedTaxonomy?.Name] + ']?'"
                                 (onCancel)="showDelete=false;"
                ></d3s-delete-form>
            </div>
        </div>
        <div *ngIf="!showEditor && !showDelete" class="row">
            <div class="col l4 s12">
                <div class="tile tile-detail">
                    <header *ngIf="!showEditor">Models
                        <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true"
                                          [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                                       (input)="dt.filterGlobal($event.target.value, 'contains')"
                                       placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="taxonomies" selectionMode="single" [metaKeySelection]="true"
                                         [globalFilterFields]="['Name','MaximumDepth']" sortField="Name" [sortOrder]="1"
                                         [pageLinks]="3" [paginator]="true" [rows]="10"
                                         [(selection)]="selectedTaxonomy">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'MaximumDepth'" style="width: 100px">
                                                Max Depth
                                                <d3s-sortIcon [field]="'MaximumDepth'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'"
                                                                   [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'MaximumDepth'"
                                                                   [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selectedTaxonomy=item;showEditor=true;" [pSelectableRow]="item">
                                            <td>{{item.Name}}</td>
                                            <td>{{item.MaximumDepth}}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;"
                                                       (click)="selectedTaxonomy=item;showEditor=true"><i
                                                            class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;"
                                                       (click)="selectedTaxonomy=item;showDelete=true"><i
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
                </div>
            </div>
            <div class="col l8 s12" *ngIf="selectedTaxonomy">
                <d3s-admin-model-detail-component [(taxonomy)]="selectedTaxonomy"></d3s-admin-model-detail-component>
            </div>
        </div>
    `
})

export class AdminTaxonomiesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    taxonomies: Taxonomy[] = [];
    error: any;
    selectedTaxonomy: Taxonomy = null;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    assetTypeClass: AssetTypeClass = AssetTypeClass.Model;
    protected assetTypeService: AssetTypeService = null;

    constructor(private stateService: StateService,
        assetTypeService: AssetTypeService,
        rightSidebarService: RightSidebarService,
        private taxonomiesService: TaxonomiesService,
        private fieldsService: FieldsService,
        private messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {

        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.assetTypeService = assetTypeService;

        this.areaName = "Models";
        this.tabTitle = "Model Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/TaxonomyType/${this.selectedTaxonomy.ID}`
            });
        }
    }

    ngOnInit() {
        this.getTaxonomies();
        this.theDeleteCallback = this.deleteTaxonomy.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getTaxonomies() {
        this.isLoading = true;
        this.taxonomiesService
            .getTaxonomies()
            .subscribe(taxonomies => {
                this.taxonomies = taxonomies.sort((a, b) => a.Name.localeCompare(b.Name));
                if (this.taxonomies.length && this.taxonomies.length > 0) {
                    this.selectedTaxonomy = this.taxonomies[0];
                }
                this.isLoading = false;
            }, error => this.error = error);
    }


    add() {
        this.selectedTaxonomy = null;
        this.showEditor = true;
    }

    closeEditor() {
        this.showEditor = false;

        if (this.selectedTaxonomy == null && this.taxonomies.length > 0) {
            this.selectedTaxonomy = this.taxonomies[0];
        }
    }

    saveModel(event) {
        this.showEditor = false;
        this.getTaxonomies();
        this.stateService.reloadLeftNavMenu();
    }

    deleteTaxonomy(id: number) {
        this
            .assetTypeService
            .deleteAssetTypeOld(id)
            .subscribe(res => {
                this.showMessageForResult(this.messagesService, res);

                if (res.type != 'error') {
                    this.taxonomies = this.taxonomies.filter(x => x.AssetTypeID != id);
                    this.selectedTaxonomy = this.taxonomies.length > 0 ? this.taxonomies[0] : null;
                    this.stateService.reloadLeftNavMenu();
                }

                this.showDelete = false;
            })
            ;
    }
}
