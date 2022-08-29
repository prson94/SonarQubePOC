import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AssetGridTopLevelListComponent } from '../assets-grid/asset-grid-top-level-list.component';
import { HierarchyListComponent } from '../hierarchy/hierarchy-list.component';
import { AssetsBaseComponent } from './assets-base.component';

const routes: Routes = [
    { path: ':assetTypeUid', component: AssetsBaseComponent },
	{ path: 'class/BusinessAsset', data: { type: "BusinessAsset" }, component: AssetGridTopLevelListComponent },
	{ path: 'class/TechnicalAsset', data: { type: "TechnicalAsset" }, component: AssetGridTopLevelListComponent },
	{ path: 'class/Rule', data: { type: "Rule" }, component: AssetGridTopLevelListComponent },
	{ path: 'class/Model', data: { type: "Model" }, component: HierarchyListComponent },
	{ path: 'class/Policy', data: { type: "Policy" }, component: HierarchyListComponent },
	{ path: 'class/Policy', data: { type: "Policy" }, component: HierarchyListComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AssetsBaseRoutingModule { }