import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { DiagramComponent } from './diagram.component';
import { BrowserComponent } from './browser.component';
import { DeactivateGuard } from '../../../guards/deactivate.guard';

const routes: Routes = [
    { path: 'browser/:assetUid', component: BrowserComponent, canDeactivate: [DeactivateGuard] },
    { path: 'browser/:assetUid/:diagramType', component: BrowserComponent, canDeactivate:[DeactivateGuard] },
    { path: 'browser/:assetUid/:diagramType/:focusKey', component: BrowserComponent, canDeactivate:[DeactivateGuard] },
    { path: 'diagram/:objectId', component: DiagramComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class VisualizationRoutingModule { }