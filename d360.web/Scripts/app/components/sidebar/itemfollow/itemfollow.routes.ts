import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ItemFollowComponent } from './itemfollow.component';

const routes: Routes = [
    { path: ':resourceID', component: ItemFollowComponent},
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ItemFollowRoutingModule { }