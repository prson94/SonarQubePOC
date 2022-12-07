import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminHierarchiesComponent } from './admin-hierarchies.component';

const routes: Routes = [
    { path: '', component: AdminHierarchiesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminHierarchiesRoutingModule { }