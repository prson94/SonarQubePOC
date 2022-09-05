import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { BrowserComponent } from './browser.component';
import { DeactivateGuard } from '../../../guards/deactivate.guard';

const routes: Routes = [
	{ path: ':assetUid/diagrams', component: BrowserComponent, canDeactivate: [DeactivateGuard] },
	{ path: ':assetUid/diagrams/:diagramType', component: BrowserComponent, canDeactivate:[DeactivateGuard] },
	{ path: ':assetUid/diagrams/:diagramType/:focusKey', component: BrowserComponent, canDeactivate:[DeactivateGuard] }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class VisualizationRoutingModule { }