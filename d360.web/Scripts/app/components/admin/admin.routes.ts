import * as admin from './index'

export const AdminRoutes = [
    {
        path: 'a/admin',
        component: admin.AdminComponent,
        // index: true,
        children: [
            { path: 'analytics', component: admin.AdminAnalyticsComponent },
            { path: 'settings', component: admin.AdminSettingsComponent },
            { path: 'domain', component: admin.AdminDomainComponent },
            { path: 'groups', component: admin.AdminGroupsComponent },
            { path: 'workflow', component: admin.AdminWorkflowComponent },
            { path: 'responsibilities', component: admin.AdminGovernanceComponent },
            { path: 'artifacts', component: admin.AdminArtifactsComponent },
            { path: 'templates', component: admin.AdminTemplatesComponent },
            { path: 'taxonomies', component: admin.AdminTaxonomiesComponent },
            { path: 'lookups', component: admin.AdminLookupsComponent },
            { path: 'rules', component: admin.AdminRulesComponent },
            { path: 'policies', component: admin.AdminPoliciesComponent },
            { path: 'attributes', component: admin.AdminAttributesComponent },
            { path: 'relationships', component: admin.AdminRelationshipsComponent },
            { path: 'resources', component: admin.AdminResourcesComponent },
        ]
    }
];