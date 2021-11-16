import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-score',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="score-tile-detail">
                       <d3s-asset-score *ngIf="show" [uid]="uid" [scoreType]="scoreType"></d3s-asset-score>
                    </div>
                </div>
            </div>
        `
})
export class ScoreComponent extends BaseComponent implements OnInit, OnDestroy {

    @Input() uid: string = "";
    @Input() scoreType: string = "";

    private sub: any;
    hasCloseButton: boolean = false;
    show: boolean = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {

        this.isLoading = true;
        this.show = false;

        this.sub = this.route.params.subscribe((params) => {
            this.uid = params['Uid'];
            this.scoreType = params['scoreType'];

            if (!this.scoreType || this.scoreType === "") {
                this.scoreType = "Governance";
            }

            this.isLoading = false;
            this.show = true;
        });
        this.buildSecondaryNavigation(this.uid);
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}