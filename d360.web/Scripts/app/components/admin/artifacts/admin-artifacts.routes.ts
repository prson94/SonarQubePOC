import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminArtifactsComponent } from './admin-artifacts.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

const routes: Routes = [
    { path: `:class`, component: AdminArtifactsComponent },
    //{ path: `${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`, component: AdminArtifactsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminArtifactsRoutingModule { }