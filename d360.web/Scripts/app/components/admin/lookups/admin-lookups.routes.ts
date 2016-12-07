import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminLookupsComponent } from './admin-lookups.component';

const routes: Routes = [
    { path: ':lookupTypeId', component: AdminLookupsComponent },
    { path: ':lookupTypeId/:lookupId', component: AdminLookupsComponent },
    { path: '', component: AdminLookupsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminLookupRoutingModule { }

