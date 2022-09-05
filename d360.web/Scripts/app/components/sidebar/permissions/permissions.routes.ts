import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { PermissionsComponent } from './permissions.component';

const routes: Routes = [
	{ path: ':assetTypeUId/owners', component: PermissionsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PermissionsRoutingModule { }