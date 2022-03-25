import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminBrandingComponent } from './admin-branding.component';

const routes: Routes = [
    { path: '', component: AdminBrandingComponent }    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminBrandingRoutingModule { }