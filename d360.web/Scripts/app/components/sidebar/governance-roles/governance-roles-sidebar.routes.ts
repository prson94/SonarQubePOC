import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { GovernanceRolesComponent } from './governance-roles-sidebar.component';

const routes: Routes = [
    { path: '', component: GovernanceRolesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class GovernanceRolesRoutingModule { }