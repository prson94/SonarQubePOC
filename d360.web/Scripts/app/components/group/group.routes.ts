import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { GroupComponent } from './group.component';
import { GroupListComponent } from './group-list.component';

const routes: Routes = [
    {
        path: '',
        component: GroupComponent,
        children: [
            { path: '', component: GroupListComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class GroupRoutingModule { }

