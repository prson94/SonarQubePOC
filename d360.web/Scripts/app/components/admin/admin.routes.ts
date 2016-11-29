import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

import { AdminGroupsComponent } from './admin-groups.component';
import { AdminArtifactsComponent } from './admin-artifacts.component';
import { AdminTaxonomiesComponent } from './admin-taxonomies.component';
import { AdminPoliciesComponent } from './admin-policies.component';
import { AdminAttributesComponent } from './admin-attributes.component';
import { AdminResourcesComponent } from './admin-resources.component';
import { AdminFusionComponent } from './admin-fusion.component';
import { AdminComponent } from './admin.component';


const routes: Routes = [
    {
        path: '',
        component: AdminComponent,
        canActivate: [AdminUserGuard],
        children: [
            { path: SiteUrlHelpers.SITE_URL_ADMIN_FUSION, component: AdminFusionComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ARTIFACTS, component: AdminArtifactsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES, component: AdminAttributesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_GROUPS, component: AdminGroupsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_POLICIES, component: AdminPoliciesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES, component: AdminResourcesComponent },            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_MODELS, component: AdminTaxonomiesComponent },
            
            //lazy load
            { path: SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS, loadChildren: './lookups/admin-lookups.module#AdminLookupsModule?chunkName=adminLookupsChunk' }, 
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS, loadChildren: './relationships/admin-relationships.module#AdminRelationshipsModule?chunkName=adminRelationshipsChunk' }, 
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS, loadChildren: './surveys/admin-surveys.module#AdminSurveysModule?chunkName=adminSurveysChunk' },             
            { path: SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW, loadChildren: './workflow/admin-workflow.module#AdminWorkflowModule?chunkName=adminWorkflowChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD, loadChildren: './load/admin-load.module#AdminLoadModule?chunkName=adminLoadChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS, loadChildren: './settings/admin-settings.module#AdminSettingsModule?chunkName=adminSettingsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS, loadChildren: './analytics/admin-analytics.module#AdminAnalyticsModule?chunkName=adminAnalyticsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS, loadChildren: './dashboards/admin-dashboards.module#AdminDashboardsModule?chunkName=adminDashboardsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_TEMPLATES, loadChildren: './templates/admin-templates.module#AdminTemplatesModule?chunkName=adminTemplatesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RULES, loadChildren: './rules/admin-rules.module#AdminRulesModule?chunkName=adminRulesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES, loadChildren: './responsibilities/admin-responsibilities.module#AdminResponsibilitiesModule?chunkName=adminResponsibilitiesChunk' },
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRoutingModule { }

