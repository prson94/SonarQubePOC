import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RulesService } from '../../services/rules.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleType } from '../../models/rule.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { AssetGridObject } from '../assets-grid/asset-grid.model';
import { WebAnalyticsService } from '../../services/web-analytics.service';

@Component({
    selector: 'd3s-rule-list',
    providers: [GridDefinitionService, RulesService, PermissionsService, WebAnalyticsService],
    templateUrl: './rule-list.component.html'
})

export class RuleListComponent extends BaseComponent implements OnInit, OnDestroy {
    routeParamsSubscription: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    ruleTypeId: number;
    gridObject: AssetGridObject;
    ruleType: RuleType;


    constructor(private route: ActivatedRoute,
        private router: Router,
        protected rulesService: RulesService,
        protected titleService: Title,
        protected messagesService: MessagesObservableService,
        private gridDefinitionService: GridDefinitionService,
        private headerActionsService: HeaderActionsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        secondaryNavService: SecondaryNavService,
        webAnalyticsService: WebAnalyticsService,
    ) {
        super();
        this.webAnalyticsService = webAnalyticsService;
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(params => {

            this.ruleTypeId = +params['ruleTypeId'];
            this.logAction("open", "RuleType", this.ruleTypeId);
            this.currentAreaNameSubscription =
                this.headerBreadcrumbService
                    .getAreaName('RuleType', this.ruleTypeId)
                    .subscribe(result => { this.currentAreaName = result });
            this.headerBreadcrumbService.setCurrentObjectInfo('RuleType', this.ruleTypeId);

            this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleTypeId);

            this.isLoading = true;
            this.rulesService.getRuleType(this.ruleTypeId)
                .subscribe(result => {
                    this.isLoading = false;
                    this.ruleType = result;
                    this.gridObject = RuleType.AsGridObject(this.ruleType);

                    this.setObjectInfo('RuleType', this.ruleType.ID);
                    
                    this.headerBreadcrumbService.getFolderTitle('#Data Quality').then((res) => {
                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.ruleType.Name, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.ruleTypeId}`,
                            undefined,
                            'RuleType',
                            this.ruleType.ID,
                            undefined,
                            undefined,
                            true));

                        this.headerBreadcrumbService.getAssetFolderIcon('RuleType', this.ruleType.ID, this.currentAreaName ? this.currentAreaName : res).subscribe(icon => {
                            this.secondaryNavService.setCurrentArea(this.ruleType.Name, icon, 'Rules');
                            this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject('RuleType', this.ruleType.ID, this.ruleType.Name, null, true, null, this.ruleType.AssetTypeUID));
                            this.setCommonSecondaryNavTabs(false, false, this.ruleType.HasDashboards);
                        });
                        this.secondaryNavService.showHeader(true);
                    });
                    this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleTypeId);
                    this.setBrowserTitle(this.titleService, this.ruleType.Name);
                });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        if (this.currentAreaNameSubscription) {
            this.currentAreaNameSubscription.unsubscribe();
        }
        if (this.routeParamsSubscription) {
            this.routeParamsSubscription.unsubscribe();
        }
    }
};
