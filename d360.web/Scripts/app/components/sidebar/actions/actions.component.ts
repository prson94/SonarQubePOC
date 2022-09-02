import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-actions',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                     <d3s-workflow-issue-details [uid]="baseAssetUid"></d3s-workflow-issue-details>
                    </div>
                </div>
            </div>
        `
})

export class ActionsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;    
    
    constructor(
        private route: ActivatedRoute,
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
        
        this.sub = this.route.params.subscribe(params => {

			this.baseAssetUid = params['assetUid'];
            this.isLoading = false;
			//ObjectId here is actually assetid!
			this.buildSecondaryNavigationByAssetUid(this.baseAssetUid);
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}