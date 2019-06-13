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
import { RuleImplementationDetail } from '../../models/rule.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-rule-implementation',
    providers: [RulesService, PermissionsService],    
    template: `                 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-definition-tile [objectType]="'RuleImplementation'" [objectID]="implementation?.ID" [objectPermissions]="permissions" [hasAttributes]="false" (onEditComplete)="editRuleImplementation($event)"></d3s-object-definition-tile>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-rule-results-grid [implementationId]="implementation?.ID"></d3s-rule-results-grid> 
                        </div>
                    </div>
                </div>`
})

export class RuleImplementationComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;    
    private implementation: RuleImplementationDetail;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    private messages: MessageBarItem[] = [];
    private ruleTypeId: number;
    
    constructor(private rulesService: RulesService,
            private route: ActivatedRoute,
            private router: Router,
            rightSidebarService: RightSidebarService,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService,
            protected permissionsService: PermissionsService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;        
    }

    ngOnInit() {                
        this.sub = this.route.params.subscribe(params => {
            this.ruleTypeId = +params['ruleTypeId']; // (+) converts string 'id' to a number    
            let ruleId = +params['ruleId']; // (+) converts string 'id' to a number
            let implementationId = +params['implementationId']; // (+) converts string 'id' to a number            
            this.isLoading = true;

            this.headerBreadcrumbService.setCurrentObjectInfo('RuleImplementation', implementationId);
                        
            this.loadPermissions(this.permissionsService, StringConstants.ObjectRule, ruleId);

            this.load(implementationId).then(() => this.isLoading = false);
        });        

    }

    ngOnDestroy() {        
        this.sub.unsubscribe();        
        this.clearSidebar();
    }

    load(implementationId: number): Promise<any> {
        return this.rulesService.getRuleImplementation(implementationId)
            .then(result => {
                this.implementation = result;

                this.setObjectInfo('RuleImplementation', this.implementation.ID, this.implementation.Name);
                this.setCommonRightSideBar(true, false, false, false, false, false, false);
                this.rightSidebarService.showItem(<RightSidebarItem>{
                    active: false,
                    icons: ['fa-tags'],
                    tag: 'qualifiers',
                    title: 'Qualifiers',
                    url: `/quality/rule/implementation/qualifiers/detail/${this.objectID}`
                });

               
                this.setBrowserTitle(this.titleService, this.implementation.Name);
                this.buildBreadcrumb()
                this.messages = []; //clear any messages for this implementation
            });
    }
    private buildBreadcrumb() {
        this.currentAreaNameSubscription =
            this.headerBreadcrumbService
                .getAreaName('RuleType', this.ruleTypeId)
                .subscribe(result => {
                    this.headerBreadcrumbService.getFolderTitle('#Data Quality').then((res) => {
                        this.currentAreaName = result
                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res));//SiteUrlHelpers.SITE_URL_RULE_ROOT
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.implementation.RuleTypeName, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.ruleTypeId}`,
                            undefined,
                            'RuleType',
                            this.implementation.RuleTypeID,
                            undefined,
                            undefined,
                            true));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.implementation.RuleName, SiteUrlHelpers.getObjectUrl('Rule',
                            this.implementation.RuleID, this.implementation.RuleTypeID),
                            undefined,
                            'Rule',
                            this.implementation.RuleID));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.implementation.Name));

                    });

                });
    }
    editRuleImplementation(e: any) {
        this.load(e.ID);
    }

};