import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-rule-results',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                       <d3s-rule-results-grid [ruleId]="ID" [ruleUid]="Uid" [showTitle]="true"></d3s-rule-results-grid> 
                    </div>
                </div>
            </div>
        `
})

export class RuleResultsComponent extends BaseComponent implements OnInit, OnDestroy {

    @Input() ID: number;
    @Input() Uid: string;

    private sub: any;
    hasCloseButton: boolean = false;
    showBoard: boolean = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        breadcrumbService: HeaderBreadcrumbService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {

        this.isLoading = true;
        this.showBoard = false;

        this.sub = this.route.params.subscribe(params => {
            this.ID = params['ID'];
            this.Uid = params['Uid'];

            this.isLoading = false;
            this.showBoard = true;
        });
        this.buildSecondaryNavigation(this.Uid, this.ID, 'Rule');
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}