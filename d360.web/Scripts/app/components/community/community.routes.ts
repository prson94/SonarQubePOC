import { CommunityComponent } from './community.component';
import { CommunitySummaryComponent } from './community-summary.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const CommunityRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT,
        component: CommunityComponent,
        children: [
            { path: '', component: CommunitySummaryComponent }
        ]
    }
];