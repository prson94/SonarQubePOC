import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { DashboardComponent } from './dashboard.component';

const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: ':objectType/:objectId/:name', component: DashboardComponent },
	{ path: ':uid', component: DashboardComponent },
	{ path: ':uid/:assetUid', component: DashboardComponent },
	{ path: ':uid/preview', component: DashboardComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class DashboardRoutingModule { }