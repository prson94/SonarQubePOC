///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../../services/page-header.service';


@Component({
    selector: 'admin-groups',
    viewProviders: [HTTP_PROVIDERS],
    templateUrl: 'scripts/app/components/admin/admin-groups.component.html'
})

export class AdminGroupsComponent {
    http: Http;
    pageHeader: PageHeader;

    constructor(http: Http, pageHeader: PageHeader) {
        this.http = http;
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Groups';
        this.pageHeader.description = 'Here you will find groups and membership.';
    }
}