import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ImpactComponent } from './impact.component';
import { LineageComponent } from './lineage.component';
import { DiagramComponent } from './diagram.component';
import { BrowserComponent } from './browser.component';

const routes: Routes = [
    { path: 'impact/:objectType/:objectId', component: ImpactComponent },
    { path: 'lineage/:objectType/:objectId', component: LineageComponent },
    { path: 'lineage/:objectType/:objectId/:showUsageOnly', component: LineageComponent },
    { path: 'browser/:assetUid', component: BrowserComponent },
    { path: 'diagram/:objectId', component: DiagramComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class VisualizationRoutingModule { }