import { SearchComponent} from './search.component';
import { RouterModule } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const SearchRoutes = [
    { path: SiteUrlHelpers.SITE_URL_SEARCH_ROOT, component: SearchComponent }
];

export const routing = RouterModule.forChild(SearchRoutes);