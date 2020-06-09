import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ConnectorLabelsComponent } from './connector-labels-sidebar.component';

const routes: Routes = [
    { path: '', component: ConnectorLabelsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ConnectorLabelsRoutingModule { }