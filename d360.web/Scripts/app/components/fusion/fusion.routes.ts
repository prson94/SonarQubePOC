import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { FusionComponent } from './fusion.component';
import { FusionItemComponent } from './fusion-item.component';
import { FusionListComponent } from './fusion-list.component';
import { FusionAttributeItemComponent } from './fusion-attribute-item.component';
import { FusionAttributeDetailsComponent } from './fusion-attribute-details.component';
import { FusionManualLoadComponent } from './fusion-manual-load.component';
import { FusionHistoryComponent } from './fusion-history.component'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: FusionComponent,
        children: [
            { path: ':fusionId', component: FusionItemComponent },
            { path: SiteUrlHelpers.SITE_URL_FUSION_LIST, component: FusionListComponent },
            { path: SiteUrlHelpers.SITE_URL_FUSION_BY_FUSIONATTRIBUTEID + '/:fusionAttributeTypeId/:fusionAttributeId', component: FusionAttributeItemComponent },
            { path: SiteUrlHelpers.SITE_URL_FUSION_ATTRIBUTE_DETAILS + '/:type/:id/:name', component: FusionAttributeDetailsComponent },
            { path: SiteUrlHelpers.SITE_URL_FUSION_ATTRIBUTE_DETAILS + '/:type/:id/:name/:dataProfileId', component: FusionAttributeDetailsComponent },
            { path: 'manual/load/:fusionId', component: FusionManualLoadComponent },
            { path: 'history/:fusionId', component: FusionHistoryComponent },
            //lazy load
            { path: SiteUrlHelpers.SITE_URL_FUSION_RULES, loadChildren: './rules/fusion-rule.module#FusionRuleModule?chunkName=fusionRuleChunk' }, 
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class FusionRoutingModule { }