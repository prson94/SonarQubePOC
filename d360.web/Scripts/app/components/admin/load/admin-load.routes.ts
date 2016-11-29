import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminLoadComponent } from './admin-load.component';

const routes: Routes = [
    { path: '', component: AdminLoadComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminLoadRoutingModule { }

