import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { OwnershipComponent } from './ownership.component';

const routes: Routes = [
    //{ path: ':objectType/:objectId', component: OwnershipComponent },
    { path: ':assetUid/owners', component: OwnershipComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class OwnershipRoutingModule { }