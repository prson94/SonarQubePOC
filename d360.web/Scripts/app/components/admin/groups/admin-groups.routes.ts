import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminGroupsComponent } from './admin-groups.component';

const routes: Routes = [
    { path: '', component: AdminGroupsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminGroupsRoutingModule { }