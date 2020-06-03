import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminDiagramAssetModule } from './admin-diagram-asset.module';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

const routes: Routes = [
    { path: SiteUrlHelpers.SITE_URL_ADMIN_DIAGRAM_ASSETS, component: AdminDiagramAssetModule }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminDiagramAssetRoutingModule { }