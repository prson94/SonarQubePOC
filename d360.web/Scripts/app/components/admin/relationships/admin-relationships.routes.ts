import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminRelationshipsComponent } from './admin-relationships.component';

const routes: Routes = [
    { path: '', component: AdminRelationshipsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminRelationshipsRoutingModule { }

