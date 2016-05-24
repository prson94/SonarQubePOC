///<reference path="../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../page-header.service';
//import { NgTableComponent, NG_TABLE_DIRECTIVES } from 'ng2-table';

@Component({
    selector: 'admin-groups',
    viewProviders: [HTTP_PROVIDERS],
   // directives: [NG_TABLE_DIRECTIVES],
    templateUrl: 'scripts/app/templates/admin-groups.component.html'
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