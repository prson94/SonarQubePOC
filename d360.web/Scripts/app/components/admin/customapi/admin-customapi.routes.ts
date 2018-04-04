import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminCustomAPIComponent } from './admin-customapi.component';

const routes: Routes = [
    { path: '', component: AdminCustomAPIComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminCustomAPIRoutingModule { }