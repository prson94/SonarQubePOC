import * as admin from './index'
import { RouterModule } from '@angular/router';
import { AdminUserGuard } from '../../guards/admin-user.guard';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const AdminRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_ADMIN_ROOT,
        component: admin.AdminComponent,
        canActivate: [AdminUserGuard],        
        // index: true,
        children: [
            { path: SiteUrlHelpers.SITE_URL_ADMIN_FUSION, component: admin.AdminFusionComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS, component: admin.AdminStatisticsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ARTIFACTS, component: admin.AdminArtifactsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES, component: admin.AdminAttributesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD, component: admin.AdminLoadComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS, component: admin.AdminDashboardsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_DOMAIN, component: admin.AdminDomainComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_GROUPS, component: admin.AdminGroupsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS, component: admin.AdminLookupsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_POLICIES, component: admin.AdminPoliciesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS, component: admin.AdminRelationshipsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES, component: admin.AdminResourcesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES, component: admin.AdminGovernanceComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_RULES, component: admin.AdminRulesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS, component: admin.AdminSettingsComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS, component: admin.AdminSurveysComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_MODELS, component: admin.AdminTaxonomiesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_TEMPLATES, component: admin.AdminTemplatesComponent },
            { path: SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW, component: admin.AdminWorkflowComponent }
        ]
    }
];

export const routing = RouterModule.forChild(AdminRoutes);