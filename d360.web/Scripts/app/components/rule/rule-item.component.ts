import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
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

declare var CompanySettings;

@Component({
    selector: 'd3s-rule-item',
    providers: [RulesService, PermissionsService, WebAnalyticsService],
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-definition-tile [objectType]="'Rule'" [useV2Api]="true" [objectID]="rule?.ID" [objectPermissions]="permissions" (onEditComplete)="editRule($event)"></d3s-object-definition-tile>
                        </div>
                    </div>
                </div>`
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

    constructor(private rulesService: RulesService,
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        webAnalyticsService: WebAnalyticsService
    ) {
        super();

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

        this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
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
                this.headerBreadcrumbService.setCurrentObjectInfo('Rule', ruleId);
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