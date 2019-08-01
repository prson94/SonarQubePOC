import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminTagsComponent } from './admin-tags.component';

const routes: Routes = [
    { path: '', component: AdminTagsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminTagsRoutingModule { }

