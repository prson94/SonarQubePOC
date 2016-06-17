import { AdminComponent, AdminSettingsComponent, AdminDomainComponent, AdminGroupsComponent, AdminWorkflowComponent, AdminGovernanceComponent, AdminArtifactsComponent, AdminTemplatesComponent } from './index'

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
            { path: 'templates', component: AdminTemplatesComponent }
        ]
    }
];