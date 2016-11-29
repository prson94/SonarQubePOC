import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminAttributesComponent } from './admin-attributes.component';

const routes: Routes = [
    { path: '', component: AdminAttributesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminAttributesRoutingModule { }