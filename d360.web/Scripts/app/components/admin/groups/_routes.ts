import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { GroupList } from './list';
import { GroupChangeLog } from './log';
import { GroupFieldsList } from './fields';

const routes: Routes = [
	{ path: '', component: GroupList },
	{ path: 'fields', component: GroupFieldsList },
	{ path: ':uid/log', component: GroupChangeLog }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminGroupsRoutingModule { }