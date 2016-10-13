import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, RulesService, PermissionsService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleDetail } from '../../models/rule.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';


@Component({
    selector: 'd3s-rule-item',
    providers: [RulesService, PermissionsService],    
    template: ` 
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="rule?.ID" [objectName]="rule?.Name" [objectType]="'Rule'"></d3s-audit>                
                <d3s-lineage *ngIf="!isLoading && isLineageVisible" [objectID]="rule?.ID" [objectName]="rule?.Name" [objectType]="'Rule'"></d3s-lineage>
                <d3s-impact *ngIf="!isLoading && isImpactVisible" [objectID]="rule?.ID" [objectName]="rule?.Name" [objectType]="'Rule'"></d3s-impact>
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="rule?.ID" [objectType]="'Rule'" [title]="'Ownership of ' + rule?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading && isRelationshipsVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-relationships [objectType]="'Rule'" [objectID]="rule?.ID" [objectName]="selected?.Name" [objectPermissions]="permissions"></d3s-object-relationships>
                        </div>
                    </div>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible && !isRelationshipsVisible && !isLineageVisible && !isImpactVisible">                      
                        <div class="col s12">
                            <div class="row">
                                <div class="col s12">
                                     <div class="tile tile-detail" style="padding-left:0;padding-right:0;">
                                        <d3s-object-governance [objectType]="'Rule'" [objectID]="rule?.ID" [objectName]="rule?.Name"></d3s-object-governance>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col s12">
                                    <div class="tile tile-detail">
                                        <d3s-object-definition-tile [objectType]="'Rule'" [objectID]="rule?.ID" [objectPermissions]="permissions" [hasAttributes]="true" [hasSynonyms]="false"></d3s-object-definition-tile>
                                    </div>
                                </div>
                            </div>                            
                        </div>
                </div>
                `
})

export class RuleItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private rule: RuleDetail;

    constructor(private rulesService: RulesService,
            private route: ActivatedRoute,
            private router: Router,
            rightSidebarService: RightSidebarService,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService,
            protected permissionsService: PermissionsService
    ) {
        super(rightSidebarService);

        this.setCommonRightSideBar(true, true,false,true,true,true);
    }

    ngOnInit() {
        
                
        this.sub = this.route.params.subscribe(params => {
            let ruleId = +params['ruleId']; // (+) converts string 'id' to a number            
            this.isLoading = true;

            this.headerBreadcrumbService.setCurrentObjectInfo('Rule', ruleId);

            this.loadPermissions(this.permissionsService, StringConstants.ObjectRule, ruleId);

            this.rulesService.getRule(ruleId)
                .then(result => {
                    this.rule = result;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Rule', SiteUrlHelpers.SITE_URL_RULE_ROOT));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.rule.Name, undefined, true, 'Rule', this.rule.ID));

                    this.setBrowserTitle(this.titleService, this.rule.Name);
                    this.isLoading = false;
                });
        });
    }

    ngOnDestroy() {        
        this.sub.unsubscribe();
        this.clearSidebar();
    }
};