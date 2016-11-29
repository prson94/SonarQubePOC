import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminPoliciesComponent } from './admin-policies.component';

const routes: Routes = [
    { path: '', component: AdminPoliciesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminPoliciesRoutingModule { }