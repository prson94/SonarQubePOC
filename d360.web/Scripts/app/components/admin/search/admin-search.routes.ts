import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { AdminSearchComponent } from "./admin-search.component";

const routes: Routes = [
    { path: "", component: AdminSearchComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminSearchRoutingModule { }
