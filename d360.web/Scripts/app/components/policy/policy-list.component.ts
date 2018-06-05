import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PoliciesService } from '../../services/policies.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Policy, PolicyType } from '../../models/policy.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-policy-list',
    providers: [PoliciesService],
    template: `                 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>                                                
                        <div class="tile tile-detail" *ngIf="!isLoading">                            
                            <header>{{policyClassName}} Policies
                                <d3s-tile-actions [hasAdd]="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>         
                            <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                                   
                            <p-dataTable #dt sortField="Name" sortOrder="1" [globalFilter]="gb"  [value]="policies" scrollable="true" scrollWidth="100%" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showPolicyType(selected);" >
                                <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'250px'}" [filter]="!showSimpleFilter">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                            <a (click)="showPolicyType(item)">{{item.Name}}</a>
                                    </ng-template>
                                </p-column>                                                                                                                                                        
                                <p-column field="Description" header="Description" sortable="true" [filter]="!showSimpleFilter">
                                    <ng-template let-col let-data="rowData" pTemplate type="body">
                                        <span *ngIf="data.Description" [innerHtml]="data?.Description"></span>
                                    </ng-template>                                                        
                                </p-column>                              
                            </p-dataTable>      
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
        protected policiesService: PoliciesService) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.setCommonRightSideBar(true);
        
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/PolicyType/${this.selected.ID}`
            });
        }        
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Policies'));
            
            this.loadPolicies();
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    loadPolicies() {
        this.isLoading = true;
        this.policiesService.getPolicyTypes()
            .then(result => {
                this.policyClassName = '';
                this.isLoading = false;             
                this.policies = result;
                this.setBrowserTitle(this.titleService, `Policies`);
                if (this.policies.length && this.policies.length > 0) this.selected = this.policies[0];
            });
    }

    showPolicyType(policyType: PolicyType) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('POLICYTYPE', policyType.ID));
    }
};