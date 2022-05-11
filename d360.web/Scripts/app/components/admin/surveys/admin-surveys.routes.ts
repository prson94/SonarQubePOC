import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminSurveysComponent } from './admin-surveys.component';

const routes: Routes = [
    { path: '', component: AdminSurveysComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminSurveysRoutingModule { }

