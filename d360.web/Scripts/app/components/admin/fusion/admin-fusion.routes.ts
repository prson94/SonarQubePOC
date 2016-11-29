import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminFusionComponent } from './admin-fusion.component';

const routes: Routes = [
    { path: '', component: AdminFusionComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminFusionRoutingModule { }

