///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, RulesService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { AuditComponent} from '../shared/audit.component';
import { RuleDetail } from '../../models/rule.model';
import { ObjectDefinitionTile } from '../tiles/object-definition.tile';


@Component({
    selector: 'd3s-rule-item',
    directives: [AuditComponent, ObjectDefinitionTile],
    providers: [RulesService],    
    template: ` 
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="rule?.ID" [objectName]="rule?.Name" [objectType]="'Rule'"></d3s-audit>
                <div *ngIf="isLoading">
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="row" *ngIf="!isLoading && !isAuditVisible">                      
                        <div class="col s12">
                            <div class="tile tile-detail">
                                <d3s-object-definition-tile [objectType]="'Rule'" [objectID]="rule?.ID"></d3s-object-definition-tile>
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
            rightSidebarService: RightSidebarService, protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super(rightSidebarService);

        this.setCommonRightSideBar();
    }

    ngOnInit() {
        
                
        this.sub = this.route.params.subscribe(params => {
            let ruleId = +params['ruleId']; // (+) converts string 'id' to a number            
            this.isLoading = true;

            this.rulesService.getRule(ruleId)
                .then(result => {
                    this.rule = result;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Rule', 'a/rule'));
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