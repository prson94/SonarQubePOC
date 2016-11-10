import { PolicyComponent } from './policy.component';
import { PolicyItemComponent } from './policy-item.component';
import { PolicyItemStructureComponent } from './policy-item-structure.component';
import { PolicyListComponent } from './policy-list.component';

import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const PolicyRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_POLICY_ROOT,
        component: PolicyComponent,
        children: [
            { path: SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION + '/:policyTaxonomyClass', component: PolicyListComponent },
            { path: SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION, component: PolicyListComponent },
            { path: ':policyTypeId', component: PolicyItemComponent },
            { path: ':policyTypeId/structure', component: PolicyItemStructureComponent },
        ]
    }
];