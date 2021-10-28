import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-itemown-definition',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">  
                        <d3s-resource-responsibility-tile [resourceId]="resourceId" ></d3s-resource-responsibility-tile>
                    </div>
                </div>
            </div>
        `,
    providers: []
})

export class ItemOwnComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    resourceId: number;
   

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.resourceId = +params['resourceID']; // (+) converts string 'id' to a number
        });    

        this.checkSecondaryNavLocalStorage();
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}