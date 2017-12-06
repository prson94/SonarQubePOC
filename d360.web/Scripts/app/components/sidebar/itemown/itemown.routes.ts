import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ItemOwnComponent } from './itemown.component';

const routes: Routes = [
    { path: ':resourceID', component: ItemOwnComponent },    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ItemOwnRoutingModule { }