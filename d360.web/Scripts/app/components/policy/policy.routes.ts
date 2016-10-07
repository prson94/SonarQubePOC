import * as policy from './index'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const PolicyRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_POLICY_ROOT,
        component: policy.PolicyComponent,
        children: [
            { path: ':policyTypeId', component: policy.PolicyItemComponent },
            { path: ':policyTypeId/structure', component: policy.PolicyItemStructureComponent },
        ]
    }
];