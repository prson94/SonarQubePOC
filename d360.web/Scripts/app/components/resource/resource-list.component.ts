import { Component } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResourcesService } from '../../services/resources.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { UriBasedService } from '../../services/uri-based.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';

@Component({
    selector: 'd3s-resource-list',
    providers: [GridDefinitionService, UriBasedService, PermissionsService, ResourcesService],
    template: ` 
                <div class="row" *ngIf="showResources">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail">
                            <d3s-user-list></d3s-user-list>
                        </div>                        
                    </div>
                </div>
                `
})

export class ResourceListComponent extends BaseComponent {
    showResources: boolean = true;
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        secondaryNavService: SecondaryNavService) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.showResources = this.settingsService.getSettingById(CompanySettingEnum.ShowResources).BooleanSetting.Value;

        this.clearSidebar();
        this.secondaryNavService.showHeader(false);
        this.setBrowserTitle(this.titleService, 'Resource');
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Resource`));
    }
}