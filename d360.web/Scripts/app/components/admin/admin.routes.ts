import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

import { AdminArtifactsComponent } from './admin-artifacts.component';
import { AdminComponent } from './admin.component';


const routes: Routes = [
    {
        path: '',
        component: AdminComponent,
        canActivate: [AdminUserGuard],
        children: [            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ARTIFACTS, component: AdminArtifactsComponent },
                                    
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
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES, loadChildren: './resources/admin-resources.module#AdminResourcesModule?chunkName=adminResourcesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_GROUPS, loadChildren: './groups/admin-groups.module#AdminGroupsModule?chunkName=adminGroupsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_FUSION, loadChildren: './fusion/admin-fusion.module#AdminFusionModule?chunkName=adminFusionChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_POLICIES, loadChildren: './policies/admin-policies.module#AdminPoliciesModule?chunkName=adminPoliciesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_MODELS, loadChildren: './taxonomies/admin-taxonomies.module#AdminTaxonomiesModule?chunkName=adminTaxonomiesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES, loadChildren: './attributes/admin-attributes.module#AdminAttributesModule?chunkName=adminAttributesChunk' },
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRoutingModule { }

