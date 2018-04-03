import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminAnalyticsComponent } from './admin-analytics.component';
import { AdminMeasuresComponent } from './admin-measures.component';

const routes: Routes = [
    { path: '', component: AdminAnalyticsComponent },
    { path: 'measures', component: AdminMeasuresComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminAnalyticsRoutingModule { }