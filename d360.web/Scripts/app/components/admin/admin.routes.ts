import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AdminComponent } from './admin.component';

const routes: Routes = [
    {
        path: '',
        component: AdminComponent,        
        canActivate: [AdminUserGuard],
        children: [                                                
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS, loadChildren: () => import('./relationships/admin-relationships.module').then(m => m.AdminRelationshipsModule) }, 
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS, loadChildren: () => import('./surveys/admin-surveys.module').then(m => m.AdminSurveysModule) },             
            { path: SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW, loadChildren: () => import('./workflow/admin-workflow.module').then(m => m.AdminWorkflowModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD, loadChildren: () => import('./load/admin-load.module').then(m => m.AdminLoadModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS, loadChildren: () => import('./settings/admin-settings.module').then(m => m.AdminSettingsModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SCORING, loadChildren: () => import('./scoring/admin-scoring.module').then(m => m.AdminScoringModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS, loadChildren: () => import('./dashboards/admin-dashboards.module').then(m => m.AdminDashboardsModule) },            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RULES, loadChildren: () => import('./rules/admin-rules.module').then(m => m.AdminRulesModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES, loadChildren: () => import('./responsibilities/admin-responsibilities.module').then(m => m.AdminResponsibilitiesModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES, loadChildren: () => import('./resources/admin-resources.module').then( m => m.AdminResourcesModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_GROUPS, loadChildren: () => import('./groups/admin-groups.module').then( m => m.AdminGroupsModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_FUSION, loadChildren: () => import('./fusion/admin-fusion.module').then( m => m.AdminFusionModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_POLICIES, loadChildren: () => import('./hierarchies/admin-hierarchies.module').then(m => m.AdminHierarchiesModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_MODELS, loadChildren: () => import('./hierarchies/admin-hierarchies.module').then(m => m.AdminHierarchiesModule) },            
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ASSET, loadChildren: () => import('./artifacts/admin-artifacts.module').then(m => m.AdminArtifactsModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ISSUE_TYPES, loadChildren: () => import('./issuetypes/admin-issue-types.module').then(m => m.AdminIssueTypesModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_PREDICATES, loadChildren: () => import('./predicates/admin-predicates.module').then(m => m.AdminPredicatesModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ORGANIZATIONS, loadChildren: () => import('./organizations/admin-organizations.module').then(m => m.AdminOrganizationsModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_CUSTOMIZATIONS, loadChildren: () => import('./customizations/admin-customizations.module').then(m => m.AdminCustomizationsModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_CUSTOM_API, loadChildren: () => import('./customapi/admin-customapi.module').then(m => m.AdminCustomAPIModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_EXPORT_TEMPLATES, loadChildren: () => import('./exporttemplates/admin-export-templates.module').then(m => m.AdminExportTemplatesModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_TAGS, loadChildren: () => import('./tags/admin-tags.module').then(m => m.AdminTagsModule) },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_DIAGRAM_ASSETS, loadChildren: () => import('./diagram-asset/admin-diagram-asset.module').then(m => m.AdminDiagramAssetModule) },
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRoutingModule { }
