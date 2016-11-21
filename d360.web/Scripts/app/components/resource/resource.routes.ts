import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ResourceComponent } from './resource.component';
import { ResourceItemComponent } from './resource-item.component';
import { ResourceListComponent } from './resource-list.component';

const routes: Routes = [
    {
        path: '',
        component: ResourceComponent,
        children: [
            { path: '', component: ResourceListComponent },
            { path: ':resourceId', component: ResourceItemComponent }
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ResourceRoutingModule { }

