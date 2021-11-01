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
                       <d3s-asset-score *ngIf="showBoard"
                                       [uid]="uid"
                                       [objectName]="objectName"></d3s-asset-score>
                    </div>
                </div>
            </div>
        `
})

export class ScoreComponent extends BaseComponent implements OnInit, OnDestroy {

    @Input() uid: string = "";
    @Input() objectName: string = "";

    private sub: any;
    hasCloseButton: boolean = false;
    showBoard: boolean = false;

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
        this.showBoard = false;

        this.sub = this.route.params.subscribe((params) => {
            this.uid = params['Uid'];
            this.objectName = params['objectName'];

            this.isLoading = false;
            this.showBoard = true;
        });
        this.buildSecondaryNavigation(this.uid);
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}