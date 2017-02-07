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
                        <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" objectType="PolicyTypeClass"></d3s-audit>                
                        <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                            <div class="col s12">
                                <div class="tile tile-detail">   
                                    <d3s-people-responsibilities-tile [objectID]="selected?.ID" objectType="PolicyTypeClass" [title]="'Ownership of ' + selected?.Name"></d3s-people-responsibilities-tile>
                                </div>
                            </div>
                        </div>
                        <div class="tile tile-detail" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible">                            
                            <header>{{policyClassName}} Policies
                                <d3s-tile-actions [hasAdd]="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>         
                            <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                                   
                            <p-dataTable #dt sortField="PolicyTypeClass" sortOrder="1" [globalFilter]="gb"  [value]="policies" scrollable="true" scrollWidth="100%" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showPolicyType(selected);" >
                                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                <p-column field="PolicyTypeClass" [hidden]="policyTypeClassificationId" header="Classification" sortable="true" [style]="{width:'200px'}"  [filter]="!showSimpleFilter">
                                    <template let-item="rowData" pTemplate type="body">
                                            <a (click)="showPolicyTypeClass(item)">{{item.PolicyTypeClass}}</a>
                                    </template>
                                </p-column>
                                <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'200px'}" [filter]="!showSimpleFilter">
                                    <template let-item="rowData" pTemplate type="body">
                                            <a (click)="showPolicyType(item)">{{item.Name}}</a>
                                    </template>
                                </p-column>                                                                                                                                                        
                                <p-column field="Description" header="Description" sortable="true" [style]="{width:'500px'}"  [filter]="!showSimpleFilter">
                                    <template let-col let-data="rowData" pTemplate type="body">
                                        <span [innerHtml]="data?.Description"></span>
                                    </template>                                                        
                                </p-column>                              
                            </p-dataTable>      
                        </div>
                    </div>
                </div>
                `
})

export class PolicyListComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private policyTypeClassificationId: number;
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
        this.setCommonRightSideBar(true, true);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.policyTypeClassificationId = +params['policyTaxonomyClass'];

            if (this.policyTypeClassificationId > 0) {
                this.headerBreadcrumbService.setCurrentObjectInfo('PolicyTypeClass', this.policyTypeClassificationId);
            }

            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Policies', this.policyTypeClassificationId ? `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION}` : undefined));
            
            this.loadPolicies();
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    loadPolicies() {
        this.isLoading = true;
        this.policiesService.getPolicyTypesWithClassification()
            .then(result => {
                this.policyClassName = '';
                this.isLoading = false;
                if (this.policyTypeClassificationId) {
                    this.policies = result.filter(x => x.PolicyTypeClassID == this.policyTypeClassificationId);
                    this.policyClassName = this.policies.length > 0 ? this.policies[0].PolicyTypeClass : `Policy Classification ID: ${this.policyTypeClassificationId}`;
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.policyClassName));
                }
                else {
                    this.policies = result;
                }
                this.setBrowserTitle(this.titleService, `${this.policyTypeClassificationId ? this.policyClassName + ' ' : ''}Policies`);
                this.policies = _.sortBy(this.policies, 'PolicyTypeClass');
                if (this.policies.length && this.policies.length > 0) this.selected = this.policies[0];
            });
    }

    showPolicyTypeClass(policyType: PolicyType) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('POLICYTYPECLASS', policyType.PolicyTypeClassID));
    }

    showPolicyType(policyType: PolicyType) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('POLICYTYPE', policyType.ID));
    }
};