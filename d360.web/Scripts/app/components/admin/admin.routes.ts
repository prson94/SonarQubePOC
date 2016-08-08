import * as admin from './index'

export const AdminRoutes = [
    {
        path: 'a/admin',
        component: admin.AdminComponent,
        // index: true,
        children: [
            { path: 'fusion', component: admin.AdminFusionComponent},
            { path: 'analytics', component: admin.AdminStatisticsComponent },
            { path: 'artifacts', component: admin.AdminArtifactsComponent },
            { path: 'attributes', component: admin.AdminAttributesComponent },
            { path: 'load', component: admin.AdminLoadComponent},
            { path: 'dashboards', component: admin.AdminDashboardsComponent },
            { path: 'domain', component: admin.AdminDomainComponent },
            { path: 'groups', component: admin.AdminGroupsComponent },
            { path: 'lookups', component: admin.AdminLookupsComponent },
            { path: 'policies', component: admin.AdminPoliciesComponent },
            { path: 'relationships', component: admin.AdminRelationshipsComponent },
            { path: 'resources', component: admin.AdminResourcesComponent },
            { path: 'responsibilities', component: admin.AdminGovernanceComponent },
            { path: 'rules', component: admin.AdminRulesComponent },
            { path: 'settings', component: admin.AdminSettingsComponent },
            { path: 'surveys', component: admin.AdminSurveysComponent },
            { path: 'taxonomies', component: admin.AdminTaxonomiesComponent },
            { path: 'templates', component: admin.AdminTemplatesComponent },
            { path: 'workflow', component: admin.AdminWorkflowComponent }
        ]
    }
];