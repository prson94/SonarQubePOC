import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { RulesService } from '../../services/rules.service';
import { PermissionsService } from '../../services/permissions.service';
import { SurveysService } from '../../services/surveys.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleDetail, RuleImplementation, RuleType } from '../../models/rule.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { Permission } from '../../models/responsibility-type.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-rule-item',
    providers: [RulesService, PermissionsService, SurveysService],    
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <d3s-messages-bar [messages]="messages" (messageClick)="showSurvey=true"></d3s-messages-bar>
                    <div class="col s12" *ngIf="showSurvey && surveyType">
                                <div class="tile tile-detail">
                                    <d3s-take-survey [surveyType]="surveyType" [objectID]="selected?.ID" [objectType]="'Taxonomy'" (surveyCancel)="showSurvey=false" (surveyComplete)="completeSurvey()"></d3s-take-survey>
                                </div>
                    </div>
                    <div class="col s12" *ngIf="showSocialScoreBar">
                        <div class="tile tile-detail" style="padding-left:0;padding-right:0;">
                            <d3s-object-governance [uid]="rule?.Uid" [objectType]="'Rule'" [objectID]="rule?.ID" [objectName]="rule?.Name" [status]="rule?.Status"></d3s-object-governance>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-definition-tile [objectType]="'Rule'" [objectID]="rule?.ID" [objectPermissions]="permissions" [hasAttributes]="ruleType?.AllowAttributes" (onEditComplete)="editRule($event)"></d3s-object-definition-tile>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <div class="row">
                                <div class="col s12 m6 l3">
                                    <d3s-rule-implementations-grid [ruleId]="rule?.ID" [(selected)]="selectedImp"></d3s-rule-implementations-grid> 
                                </div>
                                <div class="col s12 m6 l9">
                                    <d3s-rule-implementation-summary [implementation]="selectedImp"></d3s-rule-implementation-summary>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>`
})

export class RuleItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private routeParamsSubscription: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    private rightSub: any;
    private rule: RuleDetail;
    private messages: MessageBarItem[] = [];
    private surveyType: SurveyType;
    private showSurvey: boolean = false;    
    private selectedImp: RuleImplementation;
    private showSocialScoreBar: boolean = true;
    private ruleType: RuleType;

    constructor(private rulesService: RulesService,
            private route: ActivatedRoute,
            private router: Router,
            rightSidebarService: RightSidebarService,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService,
            protected permissionsService: PermissionsService,
            protected surveysService: SurveysService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(params => {
            let ruleTypeId = +params['ruleTypeId']; // (+) converts string 'id' to a number    
            let ruleId = +params['ruleId']; // (+) converts string 'id' to a number            
            this.isLoading = true;

            this.currentAreaNameSubscription =
                this.headerBreadcrumbService
                    .getAreaName('RuleType', ruleTypeId)
                    .subscribe(result => { this.currentAreaName = result });

            this.load(ruleId).then(() => {
                this.rulesService.getRuleType(ruleTypeId).then(r => { this.ruleType = r; this.buildbreadcrumb(); });
                this.headerBreadcrumbService.setCurrentObjectInfo('Rule', ruleId);
                this.setObjectInfo('Rule', ruleId, this.rule.Name, this.rule.AssetID);

                this.loadPermissions(this.permissionsService, StringConstants.ObjectRule, ruleId).then(p => {
                    this.clearSidebar();
                    this.setCommonRightSideBar(true, this.hasPermission(Permission.ReadResponsibilities), false, true, true, this.hasPermission(Permission.ReadRelationships), true, true);
                });

                this.isLoading = false;
            });
        });

        this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
    }

    ngOnDestroy() {        
        this.routeParamsSubscription.unsubscribe(); 
        this.currentAreaNameSubscription.unsubscribe();
        this.clearSidebar();
    }

    private buildbreadcrumb() {
        this.headerBreadcrumbService.getFolderTitle('#Data Quality').then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res, undefined));//SiteUrlHelpers.SITE_URL_RULE_ROOT
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.ruleType.Name, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.ruleType.ID}`,
                undefined,
                'RuleType',
                this.ruleType.ID,
                undefined,
                undefined,
                true));
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.rule.Name,
                SiteUrlHelpers.getObjectUrl('RULEIMPLEMENTATION', this.rule.ID, this.ruleType.ID),
                true,
                'Rule',
                this.ruleType.ID));
        });
    }
    load(ruleId: number): Promise<any> {
        return this.rulesService.getRule(ruleId)
            .then(result => {
                this.rule = result;
                this.setBrowserTitle(this.titleService, this.rule.Name);
                this.messages = []; //clear any messages for this rule
                this.loadItemSurvey();
            });
    }

    editRule(e: any) {
        this.load(e.ID);
    }

    private loadItemSurvey() {

        this.surveysService.getObjectSurvey(this.rule.TypeID, 'RuleType', this.rule.ID, 'Rule')
            .subscribe(result => {
                this.surveyType = undefined;
                if (result) {
                    this.surveyType = result;
                    this.messages.push({
                        content: `<u>Click here</u> to take the survey: <em>${result.Name}</em>.`, showClose: true, data: 'Survey'
                    });
                }

            });
    }

    private completeSurvey() {
        this.showSurvey = false;
        var index = this.messages.findIndex(x => x.data == 'Survey');
        if (index >= 0 && index < this.messages.length)
            this.messages.splice(index, 1);
    }

};