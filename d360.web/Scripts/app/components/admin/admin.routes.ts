import { AdminComponent, AdminSettingsComponent, AdminDomainComponent, AdminGroupsComponent, AdminWorkflowComponent, AdminGovernanceComponent, AdminArtifactsComponent, AdminTemplatesComponent, AdminTaxonomiesComponent, AdminLookupsComponent, AdminRulesComponent, AdminPoliciesComponent, AdminAttributesComponent, AdminRelationshipsComponent } from './index'

export const AdminRoutes = [
    {
        path: 'a/admin',
        component: AdminComponent,
        // index: true,
        children: [
            { path: 'settings', component: AdminSettingsComponent },
            { path: 'domain', component: AdminDomainComponent },
            { path: 'groups', component: AdminGroupsComponent },
            { path: 'workflow', component: AdminWorkflowComponent },
            { path: 'responsibilities', component: AdminGovernanceComponent },
            { path: 'artifacts', component: AdminArtifactsComponent },
            { path: 'templates', component: AdminTemplatesComponent },
            { path: 'taxonomies', component: AdminTaxonomiesComponent },
            { path: 'lookups', component: AdminLookupsComponent },
            { path: 'rules', component: AdminRulesComponent },
            { path: 'policies', component: AdminPoliciesComponent },
            { path: 'attributes', component: AdminAttributesComponent },
            { path: 'relationships', component: AdminRelationshipsComponent }
        ]
    }
];