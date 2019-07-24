import {Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../shared/base.component';
import {Title} from '@angular/platform-browser';
import {ModelsService} from '../../services/models.service';
import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {RightSidebarService} from '../../services/right-sidebar.service';
import {Breadcrumb} from '../../models/breadcrumb.model';
import {Model} from '../../models/model.model';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-model-list',
    providers: [ModelsService],
    template: `
        <div class="row">
            <div class="col s12">
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail"
                     *ngIf="!isLoading">
                    <header>{{modelGroup}} Models
                        <d3s-tile-actions [hasAdd]="false"
                                          [hasFilterMode]="true"
                                          [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
                    </header>
                    <input type="text"
                           [hidden]="!showSimpleFilter"
                           pInputText
                           size="100"
                           (input)="dt.filterGlobal($event.target.value, 'contains')"
                           placeholder="Search..."
                           class="grid-simple-filter">
                    <p-table #dt
                             [value]="models | modelType: modelGroup"
                             selectionMode="single"
                             [metaKeySelection]="true"
                             [globalFilterFields]="['Name','Description']"
                             sortField="TaxonomyTypeClass"
                             [pageLinks]="3"
                             [paginator]="true"
                             [rows]="defaultInitialItemsPerPage"
                             [rowsPerPageOptions]="defaultPagingOptions">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'"
                                    style="width: 200px">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Description'"
                                    style="width: 500px">
                                    Description
                                    <d3s-sortIcon [field]="'Description'"></d3s-sortIcon>
                                </th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th>
                                    <d3s-column-filter [field]="'Name'"
                                                       [datatype]="'text'"></d3s-column-filter>
                                </th>
                                <th>
                                    <d3s-column-filter [field]="'Description'"
                                                       [datatype]="'text'"></d3s-column-filter>
                                </th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body"
                                     let-item>
                            <tr (dblclick)="selected=item;showModel(selected);"
                                [pSelectableRow]="item">
                                <td>
                                    <a (click)="showModel(item)">{{item.Name}}</a>
                                </td>
                                <td>
                                    <span [innerHtml]="item?.Description"></span>
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

                </div>
            </div>
        </div>
    `
})

export class ModelListComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private modelGroup: string;
    private models: Model[] = [];
    private selected: Model;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected modelsService: ModelsService) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.setObjectInfo('TaxonomyType', -1);
        this.setCommonRightSideBar(true);

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/TaxonomyType/${this.selected.ID}`
            });
        }

        if (this.ownershipSidebar) {
            this.ownershipSidebar.hasDynamicUrl = true;
            this.ownershipSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/ownership/TaxonomyType/${this.selected.ID}`
            });
        }
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.modelGroup = params['group'];

            this.loadModels();

           

            this.setBrowserTitle(this.titleService, `${this.modelGroup ? this.modelGroup + ' ' : ''}Models`);

        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    loadModels() {
        this.isLoading = true;
        this.modelsService.getModels().subscribe(
            result => {
                this.isLoading = false;
                this.models = result;
                this.models = _.sortBy(this.models, 'TaxonomyTypeClass');

                if (this.models.length && this.models.length > 0) {
                    this.selected = this.models[0];
                }
                    this.headerBreadcrumbService.getFolderTitle('#Models').then((res) => {
                        this.headerBreadcrumbService.clearCurrentObjectInfo();
                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res, this.modelGroup ? `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}` : undefined));
                        this.headerBreadcrumbService.getFolderIcon(res).then(icon => {
                            this.rightSidebarService.setCurrentArea(res, icon, 'Models');
                        });
                        this.rightSidebarService.showHeader(true);
                    });

                if (this.modelGroup) {
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.modelGroup));
                }
            }
        );
    }

    showModelType(model: Model) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TAXONOMYTYPECLASS', 0, undefined, model.TaxonomyTypeClass));
    }

    showModel(model: Model) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TAXONOMYTYPE', model.ID));
    }

};
