import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { RulesService } from '../../services/rules.service';
import { PermissionsService } from '../../services/permissions.service';
import { RuleDetail, RuleType } from '../../models/rule.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { StringConstants } from '../../static/string-constants';
import { Subscription } from 'rxjs';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';
import { AssetDetailClickType, LinkClickInterceptor } from '../../services/href-click-service';
import { SidePanelService } from '../../services/side-panel.service';
import { IOutputData } from 'angular-split';

@Component({
    selector: 'd3s-rule-item',
    providers: [RulesService, PermissionsService, WebAnalyticsService],
    templateUrl: 'rule-item.component.html'
})

export class RuleItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private routeParamsSubscription: any;
    private currentAreaName: string;
    private rightSub: any;
    private ruleSub: Subscription;
    private rule: RuleDetail;
    private messages: MessageBarItem[] = [];
    private showSurvey: boolean = false;
    private showSocialScoreBar: boolean = true;
    private ruleType: RuleType;

    hrefSub: Subscription;
    selectedAsset: any;
    selectedReferenceItem: any;
    selectedTag: any;

    sidePanelOpen: boolean = false;
    sidePanelStorageKey = 'side_panel_width_detail_rules';

    constructor(private rulesService: RulesService,
        private route: ActivatedRoute,
        private router: Router,
        private sidePanelService: SidePanelService,
        secondaryNavService: SecondaryNavService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        protected settingsService: CompanySettingsService,
        webAnalyticsService: WebAnalyticsService,
        private linkClickInterceptor: LinkClickInterceptor,
    ) {
        super(settingsService);

        this.webAnalyticsService = webAnalyticsService;
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(params => {
            let ruleTypeId = +params['ruleTypeId']; // (+) converts string 'id' to a number    
            let ruleId = +params['ruleId']; // (+) converts string 'id' to a number            
            this.isLoading = true;
            this.logAction("open", "Rule", ruleId);
            this.load(ruleId);
        });

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.linkClickInterceptor.handleEvent(this, ev);
        });

        this.showSocialScoreBar = this.settingsService.getSettingById(CompanySettingEnum.ShowSocialScoreBar).BooleanSetting.Value;
    }

    getSidePanelWidth(): number {
        return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
    }

    getSidePanelMaxWidth(): number {
        return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
    }

    getSidePanelMinWidth(): number {
        return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
    }

    onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
        this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
    }

    ngOnDestroy() {
        if (this.routeParamsSubscription) {
            this.routeParamsSubscription.unsubscribe();
        }
    }

    load(ruleId: number) {
        this.ruleSub = this.rulesService.getRule(ruleId)
            .subscribe(result => {
                this.rule = result;

                this.setBrowserTitle(this.titleService, this.rule.Name);
                this.messages = []; //clear any messages for this rule

                this.rulesService.getRuleType(this.rule.TypeID).subscribe(r => { this.ruleType = r; });
				this.headerBreadcrumbService.setCurrentObjectInfo('Rule', ruleId, null, this.rule.UID);
                this.setObjectInfo('Rule', ruleId, this.rule.Name, this.rule.AssetID, undefined, this.rule.UID);

                this.loadPermissions(this.permissionsService, StringConstants.ObjectRule, ruleId).then(p => {
                    this.buildSecondaryNavigation(this.rule.UID, null, null, null, null, null, null, this.rule.Name);
                });
                this.isLoading = false;
            });
    }

    editRule(e: any) {
        this.load(e.ID);
    }
}