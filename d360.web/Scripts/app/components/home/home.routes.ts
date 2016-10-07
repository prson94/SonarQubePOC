import { HomeComponent} from './home.component';
import { RouterModule } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const HomeRoutes = [
    { path: SiteUrlHelpers.SITE_URL_HOME_ROOT, component: HomeComponent }    
];

export const routing = RouterModule.forChild(HomeRoutes);