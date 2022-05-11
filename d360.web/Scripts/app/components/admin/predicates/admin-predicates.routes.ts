import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminPredicatesComponent } from './admin-predicates.component';

const routes: Routes = [    
    { path: '', component: AdminPredicatesComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminPredicateRoutingModule { }

