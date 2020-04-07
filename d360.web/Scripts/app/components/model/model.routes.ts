import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ModelComponent } from './model.component';
import { ModelListComponent } from './model-list.component';
import { ModelItemComponent } from './model-item.component';
import { ModelItemStructureComponent } from './model-item-structure.component';

import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: ModelComponent,
        children: [
            { path: SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION + '/:group', component: ModelListComponent },
            { path: SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION, component: ModelListComponent },
            { path: ':modelId/structure', component: ModelItemStructureComponent },
            { path: 'structure/:uid', component: ModelItemStructureComponent },
            { path: ':modelId', component: ModelItemComponent },
            { path: ':modelId/id/:id', component: ModelItemComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ModelRoutingModule { }