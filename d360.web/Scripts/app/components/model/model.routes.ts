import * as model from './index'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const ModelRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_MODEL_ROOT,
        component: model.ModelComponent,
        children: [
            { path: SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION + '/:group', component: model.ModelListComponent },
            { path: SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION, component: model.ModelListComponent },
            { path: ':modelId/structure', component: model.ModelItemStructureComponent },
            { path: ':modelId', component: model.ModelItemComponent }
        ]
    }
];