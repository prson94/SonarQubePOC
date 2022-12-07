import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminResourcesComponent } from './admin-resources.component';

const routes: Routes = [
    { path: '', component: AdminResourcesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminResourcesRoutingModule { }