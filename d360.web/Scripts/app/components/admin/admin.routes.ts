import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { AdminComponent } from './admin.component';

const routes: Routes = [
    {
        path: '',
        component: AdminComponent,        
        canActivate: [AdminUserGuard],
        children: [                                                
            { path: 'configuration/assets', loadChildren: () => import('./asset-type-configuration/asset-type-configuration.module').then((m) => m.AssetTypeConfigurationModule) }, 
            { path: 'relationships', loadChildren: () => import('./relationships/admin-relationships.module').then((m) => m.AdminRelationshipsModule) }, 
			{ path: "relationships/:assetTypeUid/fields", data: { type: 'relationship' }, loadChildren: () => import("../../components/sidebar/fields/fields.module").then((m) => m.FieldsModule) },
            { path: 'surveys', loadChildren: () => import('./surveys/admin-surveys.module').then((m) => m.AdminSurveysModule) },             
            { path: 'workflow', loadChildren: () => import('./workflow/admin-workflow.module').then((m) => m.AdminWorkflowModule) },
            { path: 'load', loadChildren: () => import('./load/admin-load.module').then((m) => m.AdminLoadModule) },
            { path: 'settings', loadChildren: () => import('./settings/admin-settings.module').then((m) => m.AdminSettingsModule) },
            { path: 'scoring', loadChildren: () => import('./scoring/admin-scoring.module').then((m) => m.AdminScoringModule) },
            { path: 'dashboard', loadChildren: () => import('./dashboards/admin-dashboards.module').then((m) => m.AdminDashboardsModule) },            
            { path: 'responsibilities', loadChildren: () => import('./responsibilities/admin-responsibilities.module').then((m) => m.AdminResponsibilitiesModule) },
            { path: 'resources', loadChildren: () => import('./resources/admin-resources.module').then( (m) => m.AdminResourcesModule) },
            { path: 'groups', loadChildren: () => import('./groups/admin-groups.module').then( (m) => m.AdminGroupsModule) },          
			{ path: 'configuration/WorkflowActions', loadChildren: () => import('./issuetypes/admin-issue-types.module').then((m) => m.AdminIssueTypesModule) },
			{ path: "predicate", loadChildren: () => import("../../components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
            { path: 'predicates', loadChildren: () => import('./predicates/admin-predicates.module').then((m) => m.AdminPredicatesModule) },
            { path: 'exporttemplates', loadChildren: () => import('./exporttemplates/admin-export-templates.module').then((m) => m.AdminExportTemplatesModule) },
            { path: 'tags', loadChildren: () => import('./tags/admin-tags.module').then((m) => m.AdminTagsModule) },
			{ path: 'branding', loadChildren: () => import('./branding/admin-branding.module').then((m) => m.AdminBrandingModule) },

        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRoutingModule { }
