import { HelpComponent } from './help.component';
import { RouterModule } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const HelpRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_HELP_ROOT,
        component: HelpComponent
    }
];