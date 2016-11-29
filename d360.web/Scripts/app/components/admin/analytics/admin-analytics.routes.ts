import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminStatisticsComponent } from './admin-statistics.component';

const routes: Routes = [
    { path: '', component: AdminStatisticsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminAnalyticsRoutingModule { }