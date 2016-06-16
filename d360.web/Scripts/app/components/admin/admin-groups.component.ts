///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../../services/page-header.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';

@Component({
    selector: 'admin-groups',
    viewProviders: [HTTP_PROVIDERS],
    templateUrl: 'scripts/app/components/admin/admin-groups.component.html'
})

export class AdminGroupsComponent {
    http: Http;
    pageHeader: PageHeader;

    constructor(http: Http, pageHeader: PageHeader, private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.http = http;
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Groups';
        this.pageHeader.description = 'Here you will find groups and membership.';

        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Groups", ""));
    }
}