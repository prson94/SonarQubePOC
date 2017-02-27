import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { MappingComponent } from './mapping.component';

const routes: Routes = [
    { path: '', component: MappingComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class MappingRoutingModule { }