import {Input, Component, EventEmitter, Output, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../shared/base.component';
import {Title} from '@angular/platform-browser';
import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {GroupService} from '../../services/group.service';
import {Breadcrumb} from '../../models/breadcrumb.model';
import {GroupSearchResultModel} from '../../models/group.model';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import { CompanySettingsService } from '../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-group-list',
    providers: [GroupService],
    template: `
        <div class="row">
            <div class="col s12">
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail">
                    <div class="row" *ngIf="!isLoading">
                        <div class="col s12">
                            <header>Groups
                                <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true"
                                                  [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
                            </header>
                            <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                                   (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                                   class="grid-simple-filter">
                            <p-table #dt [value]="groups" selectionMode="single" [metaKeySelection]="true"
                                     [globalFilterFields]="['Name','ID','NumberOfMembers']" sortField="Name"
                                     [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage"
                                     [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                                <ng-template pTemplate="header">
                                    <tr>
                                        <th [pSortableColumn]="'Name'" style="width: 60%">
                                            Name
                                            <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'ID'" style="width: 10%">
                                            ID
                                            <d3s-sortIcon [field]="'ID'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'NumberOfMembers'" style="width: 20%">
                                            Member Count
                                            <d3s-sortIcon [field]="'NumberOfMembers'"></d3s-sortIcon>
                                        </th>
                                        <th style="width:   30px "></th>
                                    </tr>
                                    <tr [hidden]="showSimpleFilter">
                                        <th>
                                            <d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter>
                                        </th>
                                        <th>
                                            <d3s-column-filter [field]="'ID'" [datatype]="'text'"></d3s-column-filter>
                                        </th>
                                        <th>
                                            <d3s-column-filter [field]="'NumberOfMembers'"
                                                               [datatype]="'text'"></d3s-column-filter>
                                        </th>
                                        <th></th>
                                    </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-item>
                                    <tr (dblclick)="selected=item;showGroup(selected);" [pSelectableRow]="item">
                                        <td>
                                            <a (click)="showGroup(item)">{{item.Name}}</a>
                                        </td>
                                        <td>{{item.ID}}</td>
                                        <td>{{item.NumberOfMembers}}</td>
                                        <td>
                                            <div class="RowTools">
                                                <a [routerLink]="groupUrl(item.ID)" style="cursor:pointer;"><i
                                                        class="fa fa-info"></i></a>
                                            </div>
                                        </td>
                                    </tr>
                                </ng-template>
                                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                                          [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                </ng-template>
                            </p-table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
})

export class GroupListComponent extends BaseComponent implements OnInit {

    private groups: GroupSearchResultModel[] = [];
    private selected: GroupSearchResultModel;

    constructor(
        private groupService: GroupService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        private route: ActivatedRoute,
        private router: Router
        ) {
        super(settingsService);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Groups');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Groups'));

        this.load();
    }

    private groupUrl(id): string {
        return SiteUrlHelpers.SITE_URL_GROUP_ROOT + '/' + id;
    }

    private load() {
        this.isLoading = true;
        this.groupService.getGroupList().subscribe(
            res => {
                this.groups = res;

                this.isLoading = false;
            }
        );
    }

    private showGroup(group) {
        if (!group) {
            console.log("ERROR : NO GROUP SELECTED TO NAVIGATE TO.");

            return;
        }
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('Group', group.ID));
    }
}
