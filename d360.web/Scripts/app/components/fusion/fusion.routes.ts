import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { FusionComponent } from './fusion.component';
import { FusionItemComponent } from './fusion-item.component';
import { FusionListComponent } from './fusion-list.component';
import { FusionAttributeItemComponent } from './fusion-attribute-item.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: FusionComponent,
        children: [
            { path: ':fusionId', component: FusionItemComponent },
            { path: SiteUrlHelpers.SITE_URL_FUSION_LIST, component: FusionListComponent },
            { path: SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID + '/:fusionAttributeTypeId/:fusionAttributeId', component: FusionAttributeItemComponent },
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class FusionRoutingModule { }

