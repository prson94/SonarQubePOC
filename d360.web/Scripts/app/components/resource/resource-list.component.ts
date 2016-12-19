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

@Component({
    selector: 'd3s-resource-list',
    providers: [GridDefinitionService, UriBasedService, PermissionsService, ResourcesService],
    template: ` 
                <div class="row">
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
    

    constructor(private route: ActivatedRoute,
        private router: Router,    
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Resources');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resources'));
    }    
};