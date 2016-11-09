import { CommunityComponent } from './community.component';
import { RouterModule } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const CommunityRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT,
        component: CommunityComponent
    }
];