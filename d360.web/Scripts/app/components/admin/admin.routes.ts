import { AdminComponent, AdminSettingsComponent, AdminDomainComponent, AdminGroupsComponent, AdminWorkflowComponent, AdminGovernanceComponent, AdminArtifactsComponent, AdminTemplatesComponent, AdminTaxonomiesComponent, AdminLookupsComponent, AdminRulesComponent, AdminPoliciesComponent, AdminAttributesComponent, AdminRelationshipsComponent, AdminAnalyticsComponent, AdminDashboardsComponent } from './index'

export const AdminRoutes = [
    {
        path: 'a/admin',
        component: AdminComponent,
        // index: true,
        children: [
            { path: 'analytics', component: AdminAnalyticsComponent },
            { path: 'artifacts', component: AdminArtifactsComponent },
            { path: 'attributes', component: AdminAttributesComponent },
            { path: 'dashboards', component: AdminDashboardsComponent },
            { path: 'domain', component: AdminDomainComponent },
            { path: 'groups', component: AdminGroupsComponent },
            { path: 'lookups', component: AdminLookupsComponent },
            { path: 'policies', component: AdminPoliciesComponent },
            { path: 'relationships', component: AdminRelationshipsComponent },
            { path: 'responsibilities', component: AdminGovernanceComponent },
            { path: 'rules', component: AdminRulesComponent },
            { path: 'settings', component: AdminSettingsComponent },                        
            { path: 'taxonomies', component: AdminTaxonomiesComponent },
            { path: 'templates', component: AdminTemplatesComponent },
            { path: 'workflow', component: AdminWorkflowComponent }
        ]
    }
];