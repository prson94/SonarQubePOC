import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { CommentsComponent } from './comments.component';

const routes: Routes = [
    { path: ':assetUid', component: CommentsComponent},
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class CommentsRoutingModule { }