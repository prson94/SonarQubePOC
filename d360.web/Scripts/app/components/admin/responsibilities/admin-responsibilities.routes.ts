import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { AdminGovernanceComponent } from "./admin-governance.component";

const routes: Routes = [
    { path: "", component: AdminGovernanceComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminResponsibilitiesRoutingModule { }