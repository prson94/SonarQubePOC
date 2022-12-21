import { Component, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { AuthenticationService } from '../../../services/authentication.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-permissions',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">
                       <d3s-responsibility-relations queryType="A" [uid]="assetTypeUId" [showAddButton]="false" [showDeleteButton]="showControls" [showEditButton]="showControls"></d3s-responsibility-relations>                        
                    </div>
                </div>
            </div>
        `,
    providers: []
})

export class PermissionsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    assetTypeUId: string;    
    title: string;
    showControls: boolean;

    constructor(private route: ActivatedRoute,
        private authenticationService: AuthenticationService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe((params) => {
            this.assetTypeUId = params['assetTypeUId'];
            this.buildSecondaryNavigationForAssetTypeUid(this.assetTypeUId);
        });
        
        this.authenticationService.checkCurrentUserAdmin().subscribe((isAdmin) => {
            this.showControls = isAdmin;
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
    
}