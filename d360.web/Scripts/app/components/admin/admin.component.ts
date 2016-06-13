///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { RouteConfig, ROUTER_DIRECTIVES, AsyncRoute } from '@angular/router-deprecated';
import { AdminSettingsComponent, AdminDomainComponent, AdminGroupsComponent, AdminWorkflowComponent, AdminGovernanceComponent, AdminArtifactsComponent, AdminTemplatesComponent } from './index'
import { PageHeader } from '../../services/page-header.service';
import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    templateUrl: 'scripts/app/components/admin/admin.component.html',
    directives: [ROUTER_DIRECTIVES],
    providers: [PageHeader]
})

@RouteConfig([
    { path: '/settings', name: 'Settings', component: AdminSettingsComponent },
    { path: '/domain', name: 'Domain', component: AdminDomainComponent },
    { path: '/groups', name: 'Groups', component: AdminGroupsComponent },
    { path: '/workflow', name: 'Workflow', component: AdminWorkflowComponent },
    { path: '/governance', name: 'Responsibilities', component: AdminGovernanceComponent },
    { path: '/artifacts', name: 'Artifacts', component: AdminArtifactsComponent},
    { path: '/templates', name: 'Templates', component: AdminTemplatesComponent },
])
export class AdminComponent {
    pageHeader: PageHeader;

    constructor(pageHeader: PageHeader) {
        this.pageHeader = pageHeader;
    }
}
