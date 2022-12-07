import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminBrandingComponent } from './admin-branding.component';

const routes: Routes = [
    { path: '', component: AdminBrandingComponent }    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminBrandingRoutingModule { }