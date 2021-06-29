import { Component, OnInit } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { StringConstants } from '../../../static/string-constants';


@Component({
    selector: 'd3s-admin-resources',
    templateUrl: './admin-resources.component.html'
})

export class AdminResourcesComponent extends AdminBaseComponent implements OnInit {
    constructor(headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, secondaryNavService: SecondaryNavService,) {
        super(headerBreadcrumbService, titleService);
        this.areaName = StringConstants.Section_Users;
        this.adminHeading = StringConstants.SubArea_Security;
        this.secondaryNavService = secondaryNavService;

        this.setCommonItems();
        this.setObjectInfo('ResourceType', 1);
        this.buildSecondaryNavigationForObject(0, 'ResourceType');
    }
    ngOnInit() {
        this.clearSidebar();
    }
}
