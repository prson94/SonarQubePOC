import { ModelComponent } from './model.component';
import { ModelListComponent } from './model-list.component';
import { ModelItemComponent } from './model-item.component';
import { ModelItemStructureComponent } from './model-item-structure.component';

import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const ModelRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_MODEL_ROOT,
        component: ModelComponent,
        children: [
            { path: SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION + '/:group', component: ModelListComponent },
            { path: SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION, component: ModelListComponent },
            { path: ':modelId/structure', component: ModelItemStructureComponent },
            { path: ':modelId', component: ModelItemComponent },
            { path: ':modelId/id/:id', component: ModelItemComponent }
        ]
    }
];