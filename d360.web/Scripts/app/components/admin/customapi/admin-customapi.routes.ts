import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminCustomAPIComponent } from './admin-customapi.component';
import { AdminCustomAPIServiceDetailComponent } from './admin-customapi-service-detail.component';

const routes: Routes = [
    { path: '', component: AdminCustomAPIComponent },
    { path: ':serviceId/details', component: AdminCustomAPIServiceDetailComponent },    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminCustomAPIRoutingModule { }