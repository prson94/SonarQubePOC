import { Component, NgZone, OnInit } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AdminBaseComponent } from '../admin-base.component';
import { FormMode } from '../../../models/form.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarService } from '../../../services/right-sidebar.service';


@Component({
    selector: 'd3s-admin-resources',
    templateUrl: './admin-resources.component.html'
})

export class AdminResourcesComponent extends AdminBaseComponent {    
    constructor(headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, rightSideBarService: RightSidebarService,) {
        super(headerBreadcrumbService, titleService);        
        this.areaName = "Users";
        this.adminHeading = "Security";
        this.rightSidebarService = rightSideBarService;
       
        this.setCommonItems();
        this.setObjectInfo('ResourceType', 1);        
    }
    ngOnInit() {
        this.clearSidebar();
    }
    resourceUri(): string {
        return `/api/resources/${this.objectID}?$orderby=LastName,FirstName`;
    }
}
