import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { PolicyComponent } from './policy.component';
import { PolicyItemComponent } from './policy-item.component';
import { PolicyItemStructureComponent } from './policy-item-structure.component';
import { PolicyListComponent } from './policy-list.component';

import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: PolicyComponent,
        children: [            
            { path: SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION, component: PolicyListComponent },
            { path: ':policyTypeId', component: PolicyItemComponent },
            { path: ':policyTypeId/structure', component: PolicyItemStructureComponent },
            { path: ':policyTypeId/id/:id', component: PolicyItemComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PolicyRoutingModule { }
