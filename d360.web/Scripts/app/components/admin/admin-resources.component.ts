
import { Component, NgZone, OnInit } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AdminBaseComponent } from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { Title } from '@angular/platform-browser';


@Component({
    selector: 'd3s-admin-resources',
    templateUrl: 'scripts/app/components/admin/admin-resources.component.html'
})

export class AdminResourcesComponent extends AdminBaseComponent {

    private objectType = 'ResourceType';
    private objectID = 1;

    constructor(pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
        this.areaDescription = "Here you will find all current resources.";
        this.areaName = "Resources";
        this.setCommonItems();
    }

    ngOnInit() {
    }

    resourceUri(): string {
        return `/api/resources/${this.objectID}?$orderby=LastName,FirstName`;
    }
}