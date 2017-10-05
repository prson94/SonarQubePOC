import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminMapsComponent } from './admin-maps.component';

const routes: Routes = [
    { path: '', component: AdminMapsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminMapsRoutingComponent { }