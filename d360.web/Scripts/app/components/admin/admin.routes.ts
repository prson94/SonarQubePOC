import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

import { AdminGovernanceComponent } from './admin-governance.component';
import { AdminSettingsComponent } from './admin-settings.component';
import { AdminGroupsComponent } from './admin-groups.component';
import { AdminWorkflowComponent } from './admin-workflow.component';
import { AdminArtifactsComponent } from './admin-artifacts.component';
import { AdminTemplatesComponent } from './admin-templates.component';
import { AdminTaxonomiesComponent } from './admin-taxonomies.component';
import { AdminRulesComponent } from './admin-rules.component';
import { AdminPoliciesComponent } from './admin-policies.component';
import { AdminAttributesComponent } from './admin-attributes.component';
import { AdminResourcesComponent } from './admin-resources.component';
import { AdminStatisticsComponent } from './admin-statistics.component';
import { AdminDashboardsComponent } from './admin-dashboards.component';
import { AdminLoadComponent } from './admin-load.component';
import { AdminFusionComponent } from './admin-fusion.component';
import { AdminSurveysComponent } from './admin-surveys.component';
import { AdminComponent } from './admin.component';


const routes: Routes = [
    {
        path: '',
        component: AdminComponent,
        canActivate: [AdminUserGuard],
        children: [
            { path: SiteUrlHelpers.SITE_URL_ADMIN_FUSION, component: AdminFusionComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS, component: AdminStatisticsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ARTIFACTS, component: AdminArtifactsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES, component: AdminAttributesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD, component: AdminLoadComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS, component: AdminDashboardsComponent },            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_GROUPS, component: AdminGroupsComponent },            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_POLICIES, component: AdminPoliciesComponent },            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES, component: AdminResourcesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES, component: AdminGovernanceComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RULES, component: AdminRulesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS, component: AdminSettingsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS, component: AdminSurveysComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_MODELS, component: AdminTaxonomiesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_TEMPLATES, component: AdminTemplatesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW, component: AdminWorkflowComponent },
            //lazy load
            { path: SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS, loadChildren: './lookups/admin-lookups.module#AdminLookupsModule?chunkName=adminLookupsChunk' }, 
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS, loadChildren: './relationships/admin-relationships.module#AdminRelationshipsModule?chunkName=adminRelationshipsChunk' }, 
            
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRoutingModule { }

