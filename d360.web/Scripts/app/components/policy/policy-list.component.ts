import {
    Component,
    OnInit,
    OnDestroy
} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';

import {Breadcrumb} from '../../models/breadcrumb.model';
import {PolicyType} from '../../models/policy.model';

import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {PoliciesService} from '../../services/policies.service';
import {RightSidebarService} from '../../services/right-sidebar.service';

import {BaseComponent} from '../shared/base.component';

import {SiteUrlHelpers} from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-policy-list',
    providers: [PoliciesService],
    template: `
        <div class="row">
            <div class="col s12">
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail"
                     *ngIf="!isLoading">
                    <header>{{ policyClassName }} Policies
                        <d3s-tile-actions [hasAdd]="false"
                                          hasFilterMode="true"
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
                             [value]="policies"
                             selectionMode="single"
                             [metaKeySelection]="true"
                             [globalFilterFields]="['Name','Description']"
                             sortField="Name"
                             [pageLinks]="3"
                             [paginator]="true"
                             [rows]="defaultInitialItemsPerPage"
                             [rowsPerPageOptions]="defaultPagingOptions"
                             [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'"
                                    style="width: 250px">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Description'">
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
                            <tr (dblclick)="selected=item;showPolicyType(selected);"
                                [pSelectableRow]="item">
                                <td>
                                    <a (click)="showPolicyType(item)">{{ item.Name }}</a>
                                </td>
                                <td>
                                    <span *ngIf="item.Description"
                                          [innerHtml]="item?.Description"></span>
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

export class PolicyListComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private policies: PolicyType[] = [];
    private selected: PolicyType;
    private policyClassName: string;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected policiesService: PoliciesService
    ) {
        super();

        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(
            params => {
                this.headerBreadcrumbService.getFolderTitle('#Policy').then((res) => {
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.clearCurrentObjectInfo();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res));
                    this.headerBreadcrumbService.getFolderIcon(res).then(icon => {
                        this.rightSidebarService.showHeader(true);
                        this.setCommonRightSideBar(true);
                        if (this.auditSidebar) {
                            this.auditSidebar.hasDynamicUrl = true;
                            this.auditSidebar.dynamicUrlCallback = (
                                () => {
                                    return `/sidebar/audit/PolicyType/${this.selected.ID}`;
                                }
                            );
                        }
                        this.rightSidebarService.setCurrentArea(res, icon, 'Policies');
                    });
                });

                this.loadPolicies();
            }
        );
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    loadPolicies() {
        this.isLoading = true;
        this.policiesService.getPolicyTypes()
            .subscribe(
                result => {
                    this.policies = result;

                    this.policyClassName = '';

                    this.setBrowserTitle(this.titleService, `Policies`);

                    if (this.policies.length && this.policies.length > 0) {
                        this.selected = this.policies[0];
                    }

                    this.isLoading = false;
                }
            );
    }

    showPolicyType(policyType: PolicyType) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('POLICYTYPE', policyType.ID));
    }
}
