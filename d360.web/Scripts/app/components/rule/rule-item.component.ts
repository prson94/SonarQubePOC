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
import { RuleDetail, RuleImplementation } from '../../models/rule.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { RightSidebarItem } from '../../models/rightsidebar.model';


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
                    <div class="col s12">
                            <div class="tile tile-detail" style="padding-left:0;padding-right:0;">
                            <d3s-object-governance [objectType]="'Rule'" [objectID]="rule?.ID" [objectName]="rule?.Name" [status]="rule?.Status"></d3s-object-governance>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-definition-tile [objectType]="'Rule'" [objectID]="rule?.ID" [objectPermissions]="permissions" [hasAttributes]="true" (onEditComplete)="editRule($event)"></d3s-object-definition-tile>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12 m6 l3">
                        <div class="tile tile-detail">
                            <d3s-rule-implementations-grid [ruleId]="rule?.ID" [(selected)]="selectedImp"></d3s-rule-implementations-grid> 
                        </div>
                    </div>
                    <div class="col s12 m6 l9">
                        <div class="tile tile-detail">
                            <d3s-rule-implementation-summary [implementation]="selectedImp"></d3s-rule-implementation-summary>
                        </div>
                    </div>
                </div>`
})

export class RuleItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private rightSub: any;
    private rule: RuleDetail;
    private messages: MessageBarItem[] = [];
    private surveyType: SurveyType;
    private showSurvey: boolean = false;    
    private selectedImp: RuleImplementation;

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
        
                
        this.sub = this.route.params.subscribe(params => {
            let ruleTypeId = +params['ruleTypeId']; // (+) converts string 'id' to a number    
            let ruleId = +params['ruleId']; // (+) converts string 'id' to a number            
            this.isLoading = true;

            this.headerBreadcrumbService.setCurrentObjectInfo('Rule', ruleId);
            this.setObjectInfo('Rule', ruleId);
            this.setCommonRightSideBar(true, true, false, true, true, true, true, true);
            this.loadPermissions(this.permissionsService, StringConstants.ObjectRule, ruleId);

            this.load(ruleId).then(() => this.isLoading = false);
        });
    }

    ngOnDestroy() {        
        this.sub.unsubscribe();        
        this.clearSidebar();
    }

    load(ruleId: number): Promise<any> {
        return this.rulesService.getRule(ruleId)
            .then(result => {
                this.rule = result;

                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Rule', undefined));//SiteUrlHelpers.SITE_URL_RULE_ROOT
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.rule.TypeName, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.rule.TypeID}`));
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.rule.Name, undefined, true, 'Rule', this.rule.ID));
                 
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
            .then(result => {
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