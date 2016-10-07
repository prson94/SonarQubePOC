import { ReferenceComponent } from './reference.component';
import { ReferenceListComponent } from './reference-list.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const ReferenceRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_REFERENCE_ROOT,
        component: ReferenceComponent,        
        children: [
            { path: '', component: ReferenceListComponent },            
        ]
    }
];