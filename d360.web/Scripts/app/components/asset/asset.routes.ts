import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ArtifactItemComponent } from '../artifact/artifact-item.component';

const routes: Routes = [
	{ path: ':assetUid', component: ArtifactItemComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AssetRoutingModule { }