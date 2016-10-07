import * as resource from './index'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const ResourceRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_RESOURCE_ROOT,
        component: resource.ResourceComponent,
        children: [
            { path: '', component: resource.ResourceListComponent },
            { path: ':resourceId', component: resource.ResourceItemComponent }
        ]
    }
];