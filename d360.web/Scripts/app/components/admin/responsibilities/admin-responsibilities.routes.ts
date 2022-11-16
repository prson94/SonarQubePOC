import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { AdminGovernanceComponent } from "./admin-governance.component";

const routes: Routes = [
    { path: "", component: AdminGovernanceComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminResponsibilitiesRoutingModule { }