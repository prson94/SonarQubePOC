import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { MessagesService } from '../../services/messages.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResourcesService } from '../../services/resources.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { UriBasedService } from '../../services/uri-based.service';
import { PermissionsService } from '../../services/permissions.service';

import { Breadcrumb } from '../../models/breadcrumb.model';
import { GridDefinition, GridColumn, GridField } from '../../models/grid-definition.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

import { RightSidebarService } from '../../services/right-sidebar.service';

declare var CompanySettings;

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
    private showResources: boolean = ((CompanySettings.ShowResources) && (CompanySettings.ShowResources.toUpperCase() == 'TRUE'));

    constructor(private route: ActivatedRoute,
        private router: Router,
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService, rightSideBarService: RightSidebarService) {
        super();
        this.rightSidebarService = rightSideBarService;
    }

    ngOnInit() {
        this.clearSidebar();
        this.setBrowserTitle(this.titleService, 'Resource');
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Resource"))
        
    }    
};