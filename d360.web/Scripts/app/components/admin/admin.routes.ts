import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { AdminComponent } from './admin.component';

const routes: Routes = [
    {
        path: '',
        component: AdminComponent,        
        canActivate: [AdminUserGuard],
        children: [                                                
            { path: 'relationships', loadChildren: () => import('./relationships/admin-relationships.module').then((m) => m.AdminRelationshipsModule) }, 
            { path: 'surveys', loadChildren: () => import('./surveys/admin-surveys.module').then((m) => m.AdminSurveysModule) },             
            { path: 'workflow', loadChildren: () => import('./workflow/admin-workflow.module').then((m) => m.AdminWorkflowModule) },
            { path: 'load', loadChildren: () => import('./load/admin-load.module').then((m) => m.AdminLoadModule) },
            { path: 'settings', loadChildren: () => import('./settings/admin-settings.module').then((m) => m.AdminSettingsModule) },
            { path: 'scoring', loadChildren: () => import('./scoring/admin-scoring.module').then((m) => m.AdminScoringModule) },
            { path: 'dashboard', loadChildren: () => import('./dashboards/admin-dashboards.module').then((m) => m.AdminDashboardsModule) },            
            { path: 'rules', loadChildren: () => import('./rules/admin-rules.module').then((m) => m.AdminRulesModule) },
            { path: 'responsibilities', loadChildren: () => import('./responsibilities/admin-responsibilities.module').then((m) => m.AdminResponsibilitiesModule) },
            { path: 'resources', loadChildren: () => import('./resources/admin-resources.module').then( (m) => m.AdminResourcesModule) },
            { path: 'groups', loadChildren: () => import('./groups/admin-groups.module').then( (m) => m.AdminGroupsModule) },
            { path: 'fusion', loadChildren: () => import('./fusion/admin-fusion.module').then( (m) => m.AdminFusionModule) },
            { path: 'policies', loadChildren: () => import('./hierarchies/admin-hierarchies.module').then((m) => m.AdminHierarchiesModule) },
            { path: 'taxonomies', loadChildren: () => import('./hierarchies/admin-hierarchies.module').then((m) => m.AdminHierarchiesModule) },            
            { path: 'assets', loadChildren: () => import('./artifacts/admin-artifacts.module').then((m) => m.AdminArtifactsModule) },
            { path: 'issuetypes', loadChildren: () => import('./issuetypes/admin-issue-types.module').then((m) => m.AdminIssueTypesModule) },
            { path: 'predicates', loadChildren: () => import('./predicates/admin-predicates.module').then((m) => m.AdminPredicatesModule) },
            { path: 'organizations', loadChildren: () => import('./organizations/admin-organizations.module').then((m) => m.AdminOrganizationsModule) },
            { path: 'customizations', loadChildren: () => import('./customizations/admin-customizations.module').then((m) => m.AdminCustomizationsModule) },
            { path: 'customapi', loadChildren: () => import('./customapi/admin-customapi.module').then((m) => m.AdminCustomAPIModule) },
            { path: 'exporttemplates', loadChildren: () => import('./exporttemplates/admin-export-templates.module').then((m) => m.AdminExportTemplatesModule) },
            { path: 'tags', loadChildren: () => import('./tags/admin-tags.module').then((m) => m.AdminTagsModule) },
            { path: 'diagrams', loadChildren: () => import('./diagram-asset/admin-diagram-asset.module').then((m) => m.AdminDiagramAssetModule) },
            { path: 'search', loadChildren: () => import('./search/admin-search.module').then((m) => m.AdminSearchModule) },
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRoutingModule { }
