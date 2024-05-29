import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { Roles } from "./roles";
//import { SecurityPolicies } from "./policies";

const routes: Routes = [
	{ path: "", component: Roles },
	{ path: "roles", component: Roles },
	//{ path: "policies", component: SecurityPolicies },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminSecurityRoutingModule { }