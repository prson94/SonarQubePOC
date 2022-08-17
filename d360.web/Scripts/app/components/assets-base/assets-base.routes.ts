import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AssetsBaseComponent } from './assets-base.component';

const routes: Routes = [
    { path: ':assetTypeUid', component: AssetsBaseComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AssetsBaseRoutingModule { }