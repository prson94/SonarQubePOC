import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminAnalyticsComponent } from './admin-analytics.component';
import { AdminAnalyticsDetailsComponent } from './admin-metric-details.component';

const routes: Routes = [
    { path: '', component: AdminAnalyticsComponent },
    { path: ':assetTypeUid/:scoreTypeEnumValue', component: AdminAnalyticsDetailsComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminAnalyticsRoutingModule { }