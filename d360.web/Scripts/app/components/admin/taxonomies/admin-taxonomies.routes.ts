import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminTaxonomiesComponent } from './admin-taxonomies.component';

const routes: Routes = [
    { path: '', component: AdminTaxonomiesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminTaxonomiesRoutingModule { }