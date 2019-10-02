import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AdminComponent } from './admin.component';
import { AdminClassificationsComponent } from './admin-classifications.component'

const routes: Routes = [
    {
        path: '',
        component: AdminComponent,        
        canActivate: [AdminUserGuard],
        children: [                                                
            //lazy load
            { path: SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS, loadChildren: './lookups/admin-lookups.module#AdminLookupsModule?chunkName=adminLookupsChunk' }, 
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS, loadChildren: './relationships/admin-relationships.module#AdminRelationshipsModule?chunkName=adminRelationshipsChunk' }, 
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS, loadChildren: './surveys/admin-surveys.module#AdminSurveysModule?chunkName=adminSurveysChunk' },             
            { path: SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW, loadChildren: './workflow/admin-workflow.module#AdminWorkflowModule?chunkName=adminWorkflowChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD, loadChildren: './load/admin-load.module#AdminLoadModule?chunkName=adminLoadChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS, loadChildren: './settings/admin-settings.module#AdminSettingsModule?chunkName=adminSettingsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS, loadChildren: './analytics/admin-analytics.module#AdminAnalyticsModule?chunkName=adminAnalyticsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS, loadChildren: './dashboards/admin-dashboards.module#AdminDashboardsModule?chunkName=adminDashboardsChunk' },            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RULES, loadChildren: './rules/admin-rules.module#AdminRulesModule?chunkName=adminRulesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES, loadChildren: './responsibilities/admin-responsibilities.module#AdminResponsibilitiesModule?chunkName=adminResponsibilitiesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES, loadChildren: './resources/admin-resources.module#AdminResourcesModule?chunkName=adminResourcesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_GROUPS, loadChildren: './groups/admin-groups.module#AdminGroupsModule?chunkName=adminGroupsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_FUSION, loadChildren: './fusion/admin-fusion.module#AdminFusionModule?chunkName=adminFusionChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_POLICIES, loadChildren: './policies/admin-policies.module#AdminPoliciesModule?chunkName=adminPoliciesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_MODELS, loadChildren: './taxonomies/admin-taxonomies.module#AdminTaxonomiesModule?chunkName=adminTaxonomiesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES, loadChildren: './attributes/admin-attributes.module#AdminAttributesModule?chunkName=adminAttributesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ASSET, loadChildren: './artifacts/admin-artifacts.module#AdminArtifactsModule?chunkName=adminArtifactsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ISSUE_TYPES, loadChildren: './issuetypes/admin-issue-types.module#AdminIssueTypesModule?chunkName=adminIssueTypesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_PREDICATES, loadChildren: './predicates/admin-predicates.module#AdminPredicatesModule?chunkName=adminPredicatesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ORGANIZATIONS, loadChildren: './organizations/admin-organizations.module#AdminOrganizationsModule?chunkName=adminOrganizationsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_CUSTOMIZATIONS, loadChildren: './customizations/admin-customizations.module#AdminCustomizationsModule?chunkName=adminCustomizationsChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_CUSTOM_API, loadChildren: './customapi/admin-customapi.module#AdminCustomAPIModule?chunkName=adminCustomAPIChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_EXPORT_TEMPLATES, loadChildren: './exporttemplates/admin-export-templates.module#AdminExportTemplatesModule?chunkName=adminExportTemplatesChunk' },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_TAGS, loadChildren: './tags/admin-tags.module#AdminTagsModule?chunkName=adminTagsChunk' },


            //static load
            { path: 'classification/:objectType', component: AdminClassificationsComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRoutingModule { }

