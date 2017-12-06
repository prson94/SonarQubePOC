import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { MemberGroupComponent } from './membergroup.component';

const routes: Routes = [
    { path: ':resourceID', component: MemberGroupComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class MemberGroupRoutingModule { }