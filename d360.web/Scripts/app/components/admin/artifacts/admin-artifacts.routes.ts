import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminArtifactsComponent } from './admin-artifacts.component';

const routes: Routes = [
    { path: `:class`, component: AdminArtifactsComponent },
    //{ path: `${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`, component: AdminArtifactsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminArtifactsRoutingModule { }