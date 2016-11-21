import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { GroupComponent } from './group.component';
import { GroupItemComponent } from './group-item.component';
import { GroupListComponent } from './group-list.component';

const routes: Routes = [
    {
        path: '',
        component: GroupComponent,
        children: [
            { path: ':groupId', component: GroupItemComponent },
            { path: '', component: GroupListComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class GroupRoutingModule { }

