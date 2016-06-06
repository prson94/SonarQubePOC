///<reference path="../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { RouteConfig, ROUTER_DIRECTIVES, AsyncRoute } from '@angular/router-deprecated';
import { AdminSettingsComponent, AdminDomainComponent, AdminGroupsComponent, AdminWorkflowComponent } from '../components/index'
import { PageHeader } from '../page-header.service';
import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    templateUrl: 'scripts/app/templates/admin.component.html',
    directives: [ROUTER_DIRECTIVES],
    providers: [PageHeader]
})

@RouteConfig([
    { path: '/settings', name: 'Settings', component: AdminSettingsComponent },
    { path: '/domain', name: 'Domain', component: AdminDomainComponent },
    { path: '/groups', name: 'Groups', component: AdminGroupsComponent },
    { path: '/workflow', name: 'Workflow', component: AdminWorkflowComponent },
])
export class AdminComponent {
    pageHeader: PageHeader;

    constructor(pageHeader: PageHeader) {
        this.pageHeader = pageHeader;
    }
}
