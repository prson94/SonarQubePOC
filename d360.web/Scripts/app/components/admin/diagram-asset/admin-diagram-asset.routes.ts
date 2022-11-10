import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminDiagramAssetComponent } from './admin-diagram-asset.component';

const routes: Routes = [
    { path: '', component: AdminDiagramAssetComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminDiagramAssetRoutingModule { }