import { FusionComponent } from './fusion.component';
import { FusionItemComponent } from './fusion-item.component';
import { FusionListComponent } from './fusion-list.component';
import { FusionAttributeItemComponent } from './fusion-attribute-item.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const FusionRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_FUSION_ROOT,
        component: FusionComponent,        
        children: [
            { path: ':fusionId', component: FusionItemComponent },            
            { path: SiteUrlHelpers.SITE_URL_FUSION_LIST, component: FusionListComponent },
            { path: SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID + '/:fusionAttributeTypeId/:fusionAttributeId', component: FusionAttributeItemComponent },
        ]
    }
];