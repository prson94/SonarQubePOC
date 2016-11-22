import { ReferenceListComponent } from './reference-list.component';
import { ReferenceComponent } from './reference.component';

import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const ReferenceRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_REFERENCE_ROOT,
        component: ReferenceComponent,
        children: [         
            { path: ':referenceListId', component: ReferenceListComponent },
            { path: '', component: ReferenceListComponent },
         
        ]                
    }
];