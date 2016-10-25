import { MonitorListComponent } from './monitor-list.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const MonitorRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_MONITOR_ROOT,
        component: MonitorListComponent,        
    }
];