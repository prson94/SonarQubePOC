import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminArtifactsComponent } from './admin-artifacts.component';

const routes: Routes = [
    { path: '', component: AdminArtifactsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminArtifactsRoutingModule { }