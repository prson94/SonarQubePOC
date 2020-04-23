import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { RuleResultsComponent } from './rule-results.component';

const routes: Routes = [
    { path: ':ID/:Uid', component: RuleResultsComponent },   
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class RuleResultsRoutingModule { }