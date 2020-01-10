import {
    Component,
    OnInit,
    OnDestroy
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/api';

import { Breadcrumb } from '../../models/breadcrumb.model';
import { Policy, PolicyType } from '../../models/policy.model';
import { Permission } from '../../models/responsibility-type.model';

import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PoliciesService } from '../../services/policies.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { PermissionsService } from '../../services/permissions.service';

import { BaseComponent } from '../shared/base.component';

import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SecondaryNavItem, SecondaryNavCurrentObject } from '../../models/secondaryNav.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-policy-item',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading"
             class="row">
            <div class="col s12">
               <div class="tile tile-detail">
                   <d3s-object-definition-tile [nymTypes]="policyType?.NymTypes"
                                               [objectPermissions]="permissions"
                                               [objectType]="'Policy'"
                                               [useV2Api]="true"
                                               [objectID]="selected?.ID"
                                               [hasAttributes]="policyType.AllowAttributes"
                                               (onEditComplete)="editComplete($event)"></d3s-object-definition-tile>
               </div>
            </div>
        </div>
    `,
    providers: [PoliciesService, PermissionsService]
})

export class PolicyItemComponent extends BaseComponent implements OnInit, OnDestroy {
    policyTypeId: number;
    policies: Policy[] = [];
    policyType: PolicyType;
    selected: Policy;
    routeParamsSubscription: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    treeSub: any;

    private showSocialScoreBar = true;

    constructor(
        private headerActionsService: HeaderActionsService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private policiesService: PoliciesService,
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        private permissionsService: PermissionsService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, '- Policy');

        this.routeParamsSubscription = this.route.params.subscribe(params => {
            const newPolicyTypeId = +params['policyTypeId'];
            // if hierarchyId is passed via alternative route to workaround
            // bug with router escaping ; = and other chars.
            let hierarchyId = +params['id'];

            if (!hierarchyId) {
                hierarchyId = +params['hierarchyId'] || 0;
            }
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            if (hierarchyId != 0) {
                this.headerBreadcrumbService.setCurrentObjectInfo('Policy', hierarchyId);
            } else {
                this.headerBreadcrumbService.setCurrentObjectInfo('PolicyType', newPolicyTypeId);
            }
            this.setObjectInfo('Policy', hierarchyId);

            this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
                id => {
                    this.showHierarchy(id);
                }
            );

            if (this.policyTypeId != newPolicyTypeId) {
                this.policyTypeId = newPolicyTypeId;

                this.isLoading = true;
                console.log(hierarchyId);
                this.load(hierarchyId).then(
                    () => this.isLoading = false
                );
            } else {
                this.headerBreadcrumbService.popLastBreadcrumb();

                this.selectPolicyHierarchy(hierarchyId).then(
                    p => {
                    }
                );
            }

        });

        this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.routeParamsSubscription.unsubscribe();
        this.treeSub.unsubscribe();
    }

    buildBreadcrumb() {
        if (this.selected)
            this.buildSecondaryNavigation(this.selected.Uid);
    }

    load(hierarchyId: number): Promise<any> {
        return this.policiesService.getPolicyType(this.policyTypeId).toPromise()
            .then(
                result => {
                    this.policyType = result;
                    this.buildBreadcrumb();
                    this.loadPolicyItems(this.policyTypeId, hierarchyId).then(
                        n => {
                            this.setBrowserTitle(this.titleService, this.policyType.Name);
                        }
                    );
                }
            );
    }

    loadPolicyItems(policyTypeId: number, selectedHierarchyId: number): Promise<any> {
        return this.policiesService.getPolicies(policyTypeId).toPromise().then(
            r => {
                this.policies = r;
                this.preloadedTreeData = this.policies;
                this.baseTreeNodeArray = this.buildTreeNodeArrayBase(this.policies);
                this.selectPolicyHierarchy(selectedHierarchyId);
            }
        );
    }

    private editComplete(e: any) {
        this.load(e.ID);
    }


    private selectPolicyHierarchy(selectedHierarchyId: number): Promise<any> {
        console.log(selectedHierarchyId);
        if (selectedHierarchyId > 0) {
            const selArray = this.policies.filter(x => x.ID == selectedHierarchyId);

            if (selArray.length > 0) {
                this.selected = selArray[0];
            } else {
                console.log('ERROR INVALID SELECTED HIERARCHY ID SPECIFIED.', selectedHierarchyId);

                this.selected = (this.policies.length && this.policies.length > 0) ? this.policies[0] : null;
            }
        } else {
            this.selected = (this.policies.length && this.policies.length > 0) ? this.policies[0] : null;
        }

        this.assetID = this.selected.AssetID;

        this.loadPermissions(this.permissionsService, StringConstants.ObjectPolicy, this.selected.ID).then(
            p => {
                this.clearSidebar();
            }
        );

        this.buildSecondaryNavigation(this.selected.Uid);

        return Promise.resolve(null);
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyTypeId};hierarchyId=${id}`);
        this.buildBreadcrumb();
    }
}
