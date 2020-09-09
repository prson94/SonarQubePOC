import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ConnectorLabelComponent } from './connector-label.component';
import { ConnectorLabelItemComponent } from './connector-label-item.component';

const routes: Routes = [
    {
        path: '',
        component: ConnectorLabelComponent,
        children: [
            { path: ':labelUid', component: ConnectorLabelItemComponent }          
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ConnectorLabelRoutingModule { }

