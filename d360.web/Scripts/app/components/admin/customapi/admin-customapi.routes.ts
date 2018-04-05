import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminCustomAPIComponent } from './admin-customapi.component';
import { AdminCustomAPIServiceDetailComponent } from './admin-customapi-service-detail.component';
import { AdminCustomAPIEndpointDetailComponent } from './admin-customapi-endpoint-detail.component';

const routes: Routes = [
    { path: '', component: AdminCustomAPIComponent },
    { path: ':serviceId/details', component: AdminCustomAPIServiceDetailComponent },    
    { path: ':serviceId/details/:endpointId/details', component: AdminCustomAPIEndpointDetailComponent},
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminCustomAPIRoutingModule { }