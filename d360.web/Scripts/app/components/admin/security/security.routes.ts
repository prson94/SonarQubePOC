import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { Policies } from "./policies";
import { Roles } from "./roles";

const routes: Routes = [
	{ path: "", component: Roles },
	{ path: "roles", component: Roles },
	{ path: "policies", component: Policies },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule], 
})
export class AdminSecurityRoutingModule { }