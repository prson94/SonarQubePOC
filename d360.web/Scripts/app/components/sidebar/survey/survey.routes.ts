import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { SurveyComponent } from './survey.component';

const routes: Routes = [
    { path: ':objectType/:objectTypeId/:objectName/:objectId', component: SurveyComponent},
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class SurveyRoutingModule { }