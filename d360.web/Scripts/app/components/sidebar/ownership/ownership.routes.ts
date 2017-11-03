import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { OwnershipComponent } from './ownership.component';

const routes: Routes = [
    //{ path: ':objectType/:objectId', component: OwnershipComponent },
    { path: ':assetID', component: OwnershipComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class OwnershipRoutingModule { }