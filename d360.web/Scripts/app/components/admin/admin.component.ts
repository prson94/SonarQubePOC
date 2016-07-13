///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
//import { RouteConfig, ROUTER_DIRECTIVES } from '@angular/router-deprecated';
import { ROUTER_DIRECTIVES } from '@angular/router';
import { AdminSettingsComponent, AdminDomainComponent, AdminGroupsComponent, AdminWorkflowComponent, AdminGovernanceComponent, AdminArtifactsComponent, AdminTemplatesComponent, AdminTaxonomiesComponent } from './index'

import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    templateUrl: 'scripts/app/components/admin/admin.component.html',
    directives: [ROUTER_DIRECTIVES]    
})

export class AdminComponent {    
   // constructor(private pageHeader: PageHeader) {    }
}
