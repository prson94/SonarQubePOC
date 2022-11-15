import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminGroupsComponent } from './admin-groups.component';

const routes: Routes = [
    { path: '', component: AdminGroupsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminGroupsRoutingModule { }