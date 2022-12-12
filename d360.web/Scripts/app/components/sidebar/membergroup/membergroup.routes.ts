import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MemberGroupComponent } from './membergroup.component';

const routes: Routes = [
    { path: ':resourceUid/groups', component: MemberGroupComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class MemberGroupRoutingModule { }