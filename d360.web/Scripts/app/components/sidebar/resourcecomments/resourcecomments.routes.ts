import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ResourceCommentsComponent } from './resourcecomments.component';

const routes: Routes = [
    { path: ':resourceID', component: ResourceCommentsComponent},
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ResourceCommentsRoutingModule { }