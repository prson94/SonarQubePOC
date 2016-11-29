import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminDashboardsComponent } from './admin-dashboards.component';

const routes: Routes = [
    { path: '', component: AdminDashboardsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminDashboardsRoutingModule { }